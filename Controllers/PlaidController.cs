using Going.Plaid;
using Going.Plaid.Accounts;
using Going.Plaid.Entity;
using Going.Plaid.Investments;
using Going.Plaid.Item;
using Going.Plaid.Link;
using Going.Plaid.Transactions;
using Going.Plaid.Webhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceDashboard.Data;
using PersonalFinanceDashboard.Models;
using Holding = PersonalFinanceDashboard.Models.Holding;
using Security = PersonalFinanceDashboard.Models.Security;
using Transaction = PersonalFinanceDashboard.Models.Transaction;


namespace PersonalFinanceDashboard.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlaidController : ControllerBase
    {
        private readonly PlaidClient _plaidClient;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public PlaidController(PlaidClient plaidClient, UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _plaidClient = plaidClient;
            _userManager = userManager;
            _context = context;
        }

        [HttpPost("create_link_token")]
        public async Task<IActionResult> CreateLinkToken()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                // Handle the case where the user is not logged in or null
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
            LinkTokenCreateRequest request = new LinkTokenCreateRequest
            {
                User = new LinkTokenCreateRequestUser { ClientUserId = currentUser.Id },
                ClientName = "Personal Finance Dashboard",
                Products = new[] { Products.Transactions, Products.Investments }, 
                CountryCodes = new[] { CountryCode.Us },
                Language = Language.English,
                Transactions = new LinkTokenTransactions
                {
                    DaysRequested = 730
                },
            };
            var response = await _plaidClient.LinkTokenCreateAsync(request);
            return Ok(response);
        }

        [HttpPost("exchange_public_token")]
        public async Task<IActionResult> ExchangePublicToken([FromBody] PublicTokenExchangeRequestDto requestDto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized("User is not authenticated.");
            }

            var exchangeRequest = new ItemPublicTokenExchangeRequest
            {
                PublicToken = requestDto.PublicToken
            };
            var exchangeResponse = await _plaidClient.ItemPublicTokenExchangeAsync(exchangeRequest);

            var plaidItem = new PlaidItem
            {
                UserID = currentUser.Id, // Use the user object we already fetched
                AccessToken = exchangeResponse.AccessToken,
                ItemID = exchangeResponse.ItemId
            };
            _context.PlaidItems.Add(plaidItem);
            await _context.SaveChangesAsync();

            var accountsGetRequest = new AccountsGetRequest
            {
                AccessToken = exchangeResponse.AccessToken
            };
            var accountsResponse = await _plaidClient.AccountsGetAsync(accountsGetRequest);

            foreach (var account in accountsResponse.Accounts)
            {
                var newFinancialAccount = new FinancialAccount
                {
                    AccountName = account.Name,
                    PlaidAccountID = account.AccountId, 
                    AccountType = account.Subtype.ToString(), 
                    CurrentBalance = account.Balances.Current ?? 0m,
                    UserID = currentUser.Id,
                    PlaidItemID = plaidItem.ID
                };
                _context.FinancialAccounts.Add(newFinancialAccount);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Plaid item and financial accounts saved successfully." });
        }

        [HttpPost("sync_account_transactions")]
        public async Task<IActionResult> SyncTransactions([FromBody] SyncTransactionsRequestDto requestDto)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Unauthorized("User is not authenticated.");
            }

            var plaidItem = await _context.PlaidItems
                .FirstOrDefaultAsync(pi => pi.ID == requestDto.PlaidItemId && pi.UserID == currentUser.Id);

            if (plaidItem == null)
            {
                return NotFound("Plaid item not found for the current user.");
            }

            var newTransactions = new List<Going.Plaid.Entity.Transaction>();
            var hasMore = true;
            var cursor = plaidItem.LastSyncCursor;

            while (hasMore)
            {
                TransactionsSyncRequest request = new TransactionsSyncRequest
                {
                    AccessToken = plaidItem.AccessToken,
                    Cursor = cursor
                };

                var response = await _plaidClient.TransactionsSyncAsync(request);
                newTransactions.AddRange(response.Added);
                hasMore = response.HasMore;
                cursor = response.NextCursor;
            }
        
            foreach (var transaction in newTransactions)
            {
                var financialAccount = await _context.FinancialAccounts
                    .FirstOrDefaultAsync(fa => fa.PlaidAccountID == transaction.AccountId);

                if (financialAccount == null) {
                    Console.WriteLine($"Skipping transactionbecause linked account {transaction.AccountId} was not found.");
                }

                var transactionExists = await _context.Transactions.AnyAsync(t => t.PlaidTransactionId == transaction.TransactionId);
                if (!transactionExists)
                {
                var dateValue = transaction.Date?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue;

                decimal amountToStore = transaction.Amount ?? 0m;

                
                if (transaction.PersonalFinanceCategory?.Detailed != "INCOME")
                    {
                        if (amountToStore > 0)
                        {
                            amountToStore *= -1;
                        }
                    }


                    var newTransaction = new Transaction
                    {
                        Description = transaction.Counterparties?.FirstOrDefault()?.Name,
                        Amount = amountToStore,
                        TransactionDate = DateTime.SpecifyKind(dateValue, DateTimeKind.Utc),
                        Category = transaction.PersonalFinanceCategory?.Detailed,
                        FinancialAccountId = financialAccount.ID,
                        PlaidTransactionId = transaction.TransactionId
                    };
                    _context.Transactions.Add(newTransaction);
                }
            }

            var accountsGetRequest = new AccountsGetRequest { AccessToken = plaidItem.AccessToken };
            var accountsResponse = await _plaidClient.AccountsGetAsync(accountsGetRequest);

            foreach (var plaidAccount  in accountsResponse.Accounts)
            {
                var dbAccount = await _context.FinancialAccounts.FirstOrDefaultAsync(fa => fa.PlaidAccountID == plaidAccount.AccountId && fa.UserID == currentUser.Id);
                if (dbAccount != null)
                {
                    dbAccount.CurrentBalance = plaidAccount.Balances.Current ?? dbAccount.CurrentBalance;
                }
            }

            plaidItem.LastSyncCursor = cursor;
            try
            {
                // Set a breakpoint on the line below
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            { 
                Console.WriteLine("An error occurred while saving to the database.");
                Console.WriteLine($"EF Core Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Database Error: {ex.InnerException.Message}");
                }
                throw;
            }

            return Ok(new { message = $"Synced {newTransactions.Count} new transactions." });
        }

        [HttpPost("sync-investments")]
        public async Task<IActionResult> SyncInvestments([FromBody] SyncInvestmentsRequestDto requestDto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized("User is not authenticated.");

            var plaidItem = await _context.PlaidItems
                .FirstOrDefaultAsync(pi => pi.ID == requestDto.PlaidItemId && pi.UserID == currentUser.Id);
            if (plaidItem == null) return NotFound("Linked investment item not found.");

            var request = new InvestmentsHoldingsGetRequest
            {
                AccessToken = plaidItem.AccessToken
            };
            var response = await _plaidClient.InvestmentsHoldingsGetAsync(request);

            foreach (var plaidSecurity in response.Securities)
            {
                var existingSecurity = await _context.Securities
                    .FirstOrDefaultAsync(s => s.PlaidSecurityId == plaidSecurity.SecurityId);
                if (existingSecurity == null)
                {
                    _context.Securities.Add(new Security
                    {
                        PlaidSecurityId = plaidSecurity.SecurityId,
                        TickerSymbol = plaidSecurity.TickerSymbol,
                        Name = plaidSecurity.Name,
                        Type = plaidSecurity.Type,
                        ClosePrice = plaidSecurity.ClosePrice,
                        ClosePriceAsOf = plaidSecurity.ClosePriceAsOf.HasValue ? plaidSecurity.ClosePriceAsOf.Value.ToDateTime(TimeOnly.MinValue) : null
                    });
                }
            }
            await _context.SaveChangesAsync();

            foreach (var plaidHolding in response.Holdings)
            {
                try
                {
                    var localAccount = await _context.FinancialAccounts
                        .FirstOrDefaultAsync(a => a.PlaidAccountID == plaidHolding.AccountId);

                    if (localAccount == null)
                    {
                        continue;
                    }

                    var localSecurity = await _context.Securities
                        .FirstOrDefaultAsync(s => s.PlaidSecurityId == plaidHolding.SecurityId);

                    if (localSecurity == null)
                    {
                        continue;
                    }

                    var existingHolding = await _context.Holdings
                        .FirstOrDefaultAsync(h => h.FinancialAccountId == localAccount.ID && h.SecurityId == localSecurity.ID);

                    if (existingHolding == null)
                    {
                        var newHolding = new Holding
                        {
                            FinancialAccountId = localAccount.ID,
                            SecurityId = localSecurity.ID,
                            Quantity = plaidHolding.Quantity,
                            CostBasis = plaidHolding.CostBasis ?? 0m,
                            InstitutionValue = plaidHolding.InstitutionValue
                        };
                        _context.Holdings.Add(newHolding);
                    }
                    else
                    {
                        existingHolding.Quantity = plaidHolding.Quantity;
                        existingHolding.CostBasis = plaidHolding.CostBasis ?? existingHolding.CostBasis;
                        existingHolding.InstitutionValue = plaidHolding.InstitutionValue;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    throw;
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Successfully synced {response.Holdings.Count} investment holdings." });
        }

        public class PublicTokenExchangeRequestDto
        {
            public string? PublicToken { get; set; }
        }

        public class SyncTransactionsRequestDto
        {
            public int PlaidItemId { get; set; }
        }
        public class SyncInvestmentsRequestDto
        {
            public int PlaidItemId { get; set; }
        }
    }
}
