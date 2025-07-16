using Going.Plaid.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceDashboard.Data;
using PersonalFinanceDashboard.Models;
using PersonalFinanceDashboard.Views.ViewModels;

namespace PersonalFinanceDashboard.Controllers
{
    [Authorize]
    public class BudgetsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public BudgetsController(ApplicationDbContext context, UserManager<IdentityUser> user)
        {
            _context = context;
            _userManager = user;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null) return RedirectToAction("Login", "Account");

            var budgets = await _context.Budgets
                .Where(b => b.UserID == currentUserId)
                .ToListAsync();

            var budgetViewModels = new List<BudgetViewModel>();

            DateTime currentDate = DateTime.UtcNow;
            DateTime firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var userAccountsIds = await _context.FinancialAccounts
                .Where(a => a.UserID == currentUserId)
                .Select(a => a.ID)
                .ToListAsync();

            foreach (var budget in budgets)
            {
                var actualSpending = await _context.Transactions
                    .Where(t => userAccountsIds.Contains(t.FinancialAccountId) &&
                                t.TransactionDate >= firstDayOfMonth &&
                                t.TransactionDate <= lastDayOfMonth &&
                                t.Category == budget.Category)
                    .SumAsync(t => t.Amount < 0 ? Math.Abs(t.Amount) : 0); // Only sum up negative amounts (spending)

                budgetViewModels.Add(new BudgetViewModel
                {
                    BudgetId = budget.Id,
                    Category = budget.Category,
                    BudgetAmount = budget.Amount,
                    ActualSpending = actualSpending,
                });
            }
            return View(budgetViewModels);
        }

        public async Task<IActionResult> Create()
        {
            //await PopulateCategoriesDropDownList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Category,Amount")] Models.Budget budgetCreate)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null) return RedirectToAction("Login", "Account");

            bool budgetExists = await _context.Budgets
                 .AnyAsync(b => b.UserID == currentUserId && b.Category == budgetCreate.Category);

            if (budgetExists)
            {
                ModelState.AddModelError("Category", "A budget for this category already exists.");
            }

            if (ModelState.IsValid)
            {
                budgetCreate.UserID = currentUserId;
                _context.Add(budgetCreate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // something failed redisplay form with available categories
            //await PopulateCategoriesDropDownList(budget.Category);
            return View(budgetCreate);
        }

        
    }
}
