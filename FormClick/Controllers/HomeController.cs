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
using FormClick.ViewModels;
using System.Security.Claims;


namespace FormClick.Controllers{
    [Authorize]
    public class HomeController : Controller{

        private readonly AppDBContext _appDbContext;
        private readonly ILogger<HomeController> _logger;

        public HomeController(AppDBContext appDbContext, ILogger<HomeController> logger){
            _appDbContext = appDbContext;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var userClaims = User.Identity as ClaimsIdentity;
            var userIdClaim = userClaims?.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            var userId = int.Parse(userIdClaim);

            var isAdmin = _appDbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Admin)
                .FirstOrDefault();

            var templates = _appDbContext.Templates
                .Where(t => t.DeletedAt == null
                            && (isAdmin
                                || t.Public
                                || t.TemplateAccesses.Any(ta => ta.UserId == userId)
                                || t.UserId == userId)
                            && !_appDbContext.Responses.Any(r => r.TemplateId == t.Id && r.UserId == userId))
                .OrderByDescending(t => t.CreatedAt)
                .Take(30)
                .Select(t => new TemplateViewModel
                {
                    TemplateId = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    UserId = t.User.Id,
                    UserName = t.User.Username,
                    IsOwner = t.User.Id == userId,
                    HasLiked = t.Likes.Any(l => l.UserId == userId),
                    TotalLikes = t.Likes.Count() // Obtiene el total de likes para cada template
                })
                .ToList();

            return View(templates);
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
