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



        public IActionResult Index()
        {
            return View();
        }
    }
}
