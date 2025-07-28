using Going.Plaid.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

            var today = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

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
            await PopulateCategoriesDropDownList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Category,Amount")] Models.Budget budgetCreate)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null) 
            {
                return RedirectToAction("Login", "Account");
            }

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
            await PopulateCategoriesDropDownList(budgetCreate.Category);
            return View(budgetCreate);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var budgetToUpdate = await _context.Budgets
                .FirstOrDefaultAsync(b => b.Id == id);

            if (budgetToUpdate == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (budgetToUpdate.UserID != currentUserId)
            {
                return Forbid();
            }

            return View(budgetToUpdate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserID,Category,Amount")] Budget newBudget)
        {
            if (id != newBudget.Id) return NotFound();

            var budgetToUpdate = await _context.Budgets
                  .FirstOrDefaultAsync(b => b.Id == id);

            if (budgetToUpdate == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (budgetToUpdate.UserID != currentUserId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    budgetToUpdate.Amount = newBudget.Amount;
                    _context.Update(budgetToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Budgets.Any(e => e.Id == budgetToUpdate.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            return View(budgetToUpdate);
        }
        private async Task PopulateCategoriesDropDownList(object selectedCategory = null)
        {
            var currentUserId = _userManager.GetUserId(User);
            var userAccountIds = await _context.FinancialAccounts
                .Where(a => a.UserID == currentUserId)
                .Select(a => a.ID)
                .ToListAsync();

            var categoriesQuery = from t in _context.Transactions
                                  where userAccountIds.Contains(t.FinancialAccountId) && t.Category != null
                                  orderby t.Category
                                  select t.Category;

            ViewBag.Categories = new SelectList(await categoriesQuery.Distinct().ToListAsync(), selectedCategory);
        }
    }
}
