using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceDashboard.Data;


namespace PersonalFinanceDashboard.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TransactionsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index(int pageNumber = 1)
        {
            var currentUserId = _userManager.GetUserId(User);
            int pageSize = 25;


            if (currentUserId == null) return RedirectToAction("Login", "Account");

            var userAccountsIds = await _context.FinancialAccounts
                .Where(a => a.UserID == currentUserId)
                .Select(a => a.ID)
                .ToListAsync();

            var query = _context.Transactions
                .Where(t => userAccountsIds.Contains(t.FinancialAccountId))
                .OrderByDescending(t => t.TransactionDate);

            var transactions = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["TotalPages"] = (int)Math.Ceiling(await query.CountAsync() / (double)pageSize);
            ViewData["CurrentPage"] = pageNumber;

            return View(transactions);
        } 
    }
}
