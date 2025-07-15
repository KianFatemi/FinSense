using System.Transactions;
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

        //this actions shows the form to ediut a single transaction
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                 .Include(t => t.FinancialAccount)
                 .FirstOrDefaultAsync(t => t.ID == id);

            if (transaction == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);

            if (transaction.FinancialAccount.UserID != currentUserId)
            {
                return Forbid();
            }

            return View(transaction);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Description,Category")] Models.Transaction transactionUpdate)
        {
            if (id != transactionUpdate.ID)
            {
                return NotFound();
            }

            var transactionToUpdate = await _context.Transactions
                .Include(t => t.FinancialAccount)
                .FirstOrDefaultAsync(t => t.ID == id);

            if (transactionToUpdate == null)
            {
                return NotFound();
            }

            //ensure transaction belongs to current user
            var currentUserId = _userManager.GetUserId(User);
            if (transactionToUpdate.FinancialAccount.UserID != currentUserId)
            { 
                return Forbid(); 
            }

            if (ModelState.IsValid)
            {
                try
                {
                    //update onlt rge properties that are editable
                    transactionToUpdate.Description = transactionUpdate.Description;
                    transactionToUpdate.Category = transactionUpdate.Category;

                    _context.Update(transactionToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Transactions.Any(e => e.ID == transactionToUpdate.ID))
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
            return View(transactionToUpdate);
        }
    } 
}

