using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceDashboard.Data;
using PersonalFinanceDashboard.Views.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalFinanceDashboard.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        
        public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSpendingByCategory()
        {
            var currentId =  _userManager.GetUserId(User);

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
            var currentId = _userManager.GetUserId(User);
            var viewModel = new DashboardDataViewModel();

            var userAccounts = await _context.FinancialAccounts
                .Where(a => a.UserID == currentId)
                .ToListAsync();

            viewModel.TotalBalance = userAccounts.Sum(a => a.CurrentBalance);
            viewModel.MainAccountBalance = userAccounts
                .Where(a => a.AccountType == "Checking")
                .Sum(a => a.CurrentBalance);
            viewModel.SavingsBalance = userAccounts
                .Where(a => a.AccountType == "Savings")
                .Sum(a => a.CurrentBalance);

            viewModel.RecentTransactions = await _context.Transactions
                .Where(t => t.FinancialAccount.UserID == currentId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(5)
                .ToListAsync();

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var releventTransactions = await _context.Transactions
                .Where(t => t.FinancialAccount.UserID == currentId && t.TransactionDate >= thirtyDaysAgo)
                .OrderBy(t => t.TransactionDate)
                .ToListAsync();

            var currentTotal = viewModel.TotalBalance;
            var runningBalance = currentTotal;

            var dailyBalances = new Dictionary<string, decimal>();

            for (int i = 0; i < 30; i++)
            {
                var date = DateTime.UtcNow.AddDays(-i);
                var transactionsForDay = releventTransactions.Where(t => t.TransactionDate == date.Date);

                runningBalance += transactionsForDay.Sum(t => t.Amount * -1);

                dailyBalances[date.ToString("MMM dd")] = runningBalance;
            }

            viewModel.AccountBalanceHistory = dailyBalances.Reverse()
                .Select(kvp => new BalanceHistoryPoint { Date = kvp.Key, Balance = kvp.Value })
                .ToList();

            return Ok(viewModel);

        }
    }
}
