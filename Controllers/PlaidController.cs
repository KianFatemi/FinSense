using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceDashboard.Data;
using Going.Plaid.Link;
using Going.Plaid;
using Going.Plaid.Entity;
using Going.Plaid.Item;
using PersonalFinanceDashboard.Models;
using Microsoft.EntityFrameworkCore;
using Going.Plaid.Transactions;
using Microsoft.AspNetCore.Authorization;
using Transaction = PersonalFinanceDashboard.Models.Transaction;
using Going.Plaid.Webhook;
using Going.Plaid.Accounts;


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
                Products = new[] { Products.Transactions }, 
                CountryCodes = new[] { CountryCode.Us },
                Language = Language.English,
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

            // --- NEW LOGIC STARTS HERE ---
            // Step 3: Use the new access_token to fetch the list of accounts from Plaid
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
                };
                _context.FinancialAccounts.Add(newFinancialAccount);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Plaid item and financial accounts saved successfully." });
        }


        public async Task<IActionResult> SyncTransactions([FromBody] SyncTransactionsRequestDto requestDto)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                // Handle the case where the user is not logged in or null
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
                    var account = await GetFinacialAccount(currentUser, plaidItem);
                    financialAccount = (FinancialAccount)account; // Ensure financialAccount is assigned
                    _context.FinancialAccounts.Add((FinancialAccount)account);
                    await _context.SaveChangesAsync();
                }

                var newTransaction = new Transaction
                {
                    Description = transaction.OriginalDescription,
                    Amount = transaction.Amount ?? 0m,
                    TransactionDate = transaction.Date.HasValue ? transaction.Date.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MinValue,
                    Category = transaction.PersonalFinanceCategory?.Detailed,
                    FinancialAccountId = financialAccount.ID
                };
                _context.Transactions.Add(newTransaction);
            }
            plaidItem.LastSyncCursor = cursor;  
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Synced {newTransactions.Count} new transactions." });
        }

        public async Task<IActionResult> GetFinacialAccount([FromBody] IdentityUser currentUser,[FromQuery] PlaidItem plaidItem)
        {
            AccountsGetRequest request = new AccountsGetRequest
            {
                AccessToken = plaidItem.AccessToken
            };

            var response = await _plaidClient.AccountsGetAsync(request);
            var newAccounts = response.Accounts;
            return Ok(newAccounts.Select(a => new FinancialAccount
            {
                AccountName = a.Name,
                AccountType = a.Type.ToString(),
                CurrentBalance = a.Balances.Current ?? 0m,
                PlaidAccountID = a.AccountId,
                UserID = currentUser.Id
            }));


        }

        public class PublicTokenExchangeRequestDto
        {
            public string? PublicToken { get; set; }
        }

        public class SyncTransactionsRequestDto
        {
            // The ID of our PlaidItem in our own database.
            public int PlaidItemId { get; set; }
        }
    }
}
