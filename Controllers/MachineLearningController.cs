using System.Reflection.PortableExecutable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceDashboard.Data;
using PersonalFinanceDashboard.ML;

namespace PersonalFinanceDashboard.Controllers
{
    [Authorize]
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class MachineLearningController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MachineLearningController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("TrainModel")]
        public async Task<IActionResult> TrainModel()
        {
            var currentUserId = _userManager.GetUserId(User);

            var userAccountsIds = await _context.FinancialAccounts
                .Where(a => a.UserID == currentUserId)
                .Select(a => a.ID)
                .ToListAsync();

            var userTransactions = await _context.Transactions
                .Where(t => userAccountsIds.Contains(t.FinancialAccountId) && t.Category != null)
                .ToListAsync();

            if (userTransactions.Count < 20)
            {
                return BadRequest(new { message = "You need at least 20 categorized transactions to train a model." });
            }

            try
            {
                var modelTrainer = new ModelTrainer();
                await Task.Run(() => modelTrainer.TrainAndSaveModel(userTransactions, currentUserId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during model training: {ex.Message}");
                return StatusCode(500, new {message = "An unexpected error occured during model training."});
            }

            return Ok(new { message = "Your AI model hs been trained successfully" });
        }
    }
}
