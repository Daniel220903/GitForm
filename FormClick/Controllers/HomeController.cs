using FormClick.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using FormClick.Data;
using Microsoft.EntityFrameworkCore;
//using AppLogin.ViewModels;
using System.Dynamic;


namespace FormClick.Controllers{
    [Authorize]
    public class HomeController : Controller{

        private readonly AppDBContext _appDbContext;
        private readonly ILogger<HomeController> _logger;

        public HomeController(AppDBContext appDbContext, ILogger<HomeController> logger){
            _appDbContext = appDbContext;
            _logger = logger;
        }

        public IActionResult Index(){
            return View();
        }

        public IActionResult Privacy(){
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(){
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
