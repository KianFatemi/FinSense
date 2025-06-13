using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity; 
using PersonalFinanceDashboard.Data; 
using System.Threading.Tasks; 
using System.Linq; 
using Microsoft.EntityFrameworkCore; 
using Microsoft.AspNetCore.Authorization;
using PersonalFinanceDashboard.Models;

//ensure only logged-in users can access these pages
[Authorize]
public class AccountsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public AccountsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        // Get the currently logged in user object
        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            // Handle the case where the user is not logged in or null
            return RedirectToAction("Login", "Account");
        }

        var financialAccounts = await _context.FinancialAccounts
                                              .Where(a => a.UserID == currentUser.Id)
                                              .ToListAsync();

        // Pass the list of accounts to the View
        return View(financialAccounts);
    }

    public ViewResult displayCreateAccountForm()
    {
        return View("Create");
    }

    public async Task<IActionResult> CreateAccount(FinancialAccount financialAccount)
    {
        if (ModelState.IsValid)
        {
            // Get the currently logged in user object
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                // Handle the case where the user is not logged in or null
                return RedirectToAction("Login", "Account");
            }
            financialAccount.UserID = currentUser.Id;
            _context.FinancialAccounts.Add(financialAccount);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(financialAccount);
    }
}