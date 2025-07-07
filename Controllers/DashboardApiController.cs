using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceDashboard.Data;
using PersonalFinanceDashboard.Views.ViewModels;

namespace PersonalFinanceDashboard.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardApiController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("GetSpendingByCategory")]
        public async Task<IActionResult> GetSpendingByCategory()
        {
            var currentId = _userManager.GetUserId(User);

            var spendingData = await _context.Transactions
                .Where(t => t.FinancialAccount.UserID == currentId)
                .Where(t => t.Amount < 0)
                .GroupBy(t => t.Category)
                .Select(group => new
                {
                    Category = group.Key,
                    TotalAmount = group.Sum(t => Math.Abs(t.Amount)),
                })
                .OrderBy(s => s.TotalAmount)
                .ToListAsync();
            return Ok(spendingData);
        }

        [HttpGet("GetDashboardData")]
        public async Task<IActionResult> GetDashboardData()
        {
            var currentUserId = _userManager.GetUserId(User);
            var viewModel = new DashboardDataViewModel();

            // Get all accounts for the user
            var userAccounts = await _context.FinancialAccounts
                .Where(a => a.UserID == currentUserId)
                .ToListAsync();

            var userAccountIds = userAccounts.Select(a => a.ID).ToList();

            viewModel.TotalBalance = userAccounts.Sum(a => a.CurrentBalance);
            viewModel.MainAccountBalance = userAccounts
                .Where(a => a.AccountType == "Checking")
                .Sum(a => a.CurrentBalance);
            viewModel.SavingsBalance = userAccounts
                .Where(a => a.AccountType == "Savings")
                .Sum(a => a.CurrentBalance);

            // Project the results into the TransactionViewModel to avoid circular references.
            viewModel.RecentTransactions = await _context.Transactions
                .Where(t => userAccountIds.Contains(t.FinancialAccountId))
                .OrderByDescending(t => t.TransactionDate)
                .Take(5)
                .Select(t => new TransactionViewModel
                {
                    Category = t.Category,
                    Amount = t.Amount,
                    TransactionDate = t.TransactionDate
                })
                .ToListAsync();

            var userHoldings = await _context.Holdings
                .Include(h => h.Security) // Include Security to get the TickerSymbol
                .Where(h => userAccountIds.Contains(h.FinancialAccountId))
                .ToListAsync();

            viewModel.InvestmentsValue = userHoldings.Sum(h => h.InstitutionValue);

            viewModel.InvestmentHoldings = userHoldings
                .GroupBy(h => h.Security.TickerSymbol) // Group holdings by ticker symbol
                .Select(group => new InvestmentHoldingViewModel
                {
                    TickerSymbol = group.Key ?? "Other", // Use "Other" if ticker is null
                    TotalValue = group.Sum(h => h.InstitutionValue)
                   })
                .OrderByDescending(h => h.TotalValue)
                .ToList();

            //Generate Account Balance History
            var thirtyDaysAgo = DateTime.UtcNow.Date.AddDays(-30);

            // Get all relevant transactions in the period
            var relevantTransactions = await _context.Transactions
                .Where(t => userAccountIds.Contains(t.FinancialAccountId) && t.TransactionDate >= thirtyDaysAgo)
                .Select(t => new { t.TransactionDate, t.Amount }) 
                .ToListAsync();

            // Calculate the balance at the start of the 30
            var sumOfPeriodTransactions = relevantTransactions.Sum(t => t.Amount);
            var startBalance = viewModel.TotalBalance - sumOfPeriodTransactions;
            
            // Group transactions by day 
            var dailyTransactionSums = relevantTransactions
                .GroupBy(t => t.TransactionDate.Date)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
            
            var balanceHistory = new List<BalanceHistoryPoint>();
            var runningBalance = startBalance;

            // Iterate forward from 30 days ago to today
            for (int i = 0; i <= 30; i++)
            {
                var date = thirtyDaysAgo.AddDays(i);
                
                // Add the net transactions for the day to the running balance
                if (dailyTransactionSums.TryGetValue(date, out var todaysSum))
                {
                    runningBalance += todaysSum;
                }
                
                balanceHistory.Add(new BalanceHistoryPoint 
                { 
                    Date = date.ToString("MMM dd"), 
                    Balance = runningBalance 
                });
            }

            viewModel.AccountBalanceHistory = balanceHistory;

            return Ok(viewModel);
        }
    }
}
