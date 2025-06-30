using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceDashboard.Data;
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
        public async Task<IActionResult> GetSpendingByCategort()
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
    }
}
