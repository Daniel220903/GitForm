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
        public IActionResult Index() {
            var userClaims = User.Identity as ClaimsIdentity;
            var userIdClaim = userClaims?.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            var userId = int.Parse(userIdClaim);

            var isAdmin = _appDbContext.Users.Where(u => u.Id == userId).Select(u => u.Admin).FirstOrDefault();

            var templates = _appDbContext.Templates.Where(t => t.DeletedAt == null
                            && t.IsCurrent == true 
                            && (isAdmin
                                || t.Public
                                || t.TemplateAccesses.Any(ta => ta.UserId == userId)
                                || t.UserId == userId)
                            && !_appDbContext.Responses.Any(r => r.TemplateId == t.Id && r.UserId == userId))      
               .OrderByDescending(t => t.CreatedAt)
               .Select(t => new TemplateViewModel {
                    TemplateId = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    Version = t.Version,
                    ProfilePicture = t.User.ProfilePicture,
                    picture = t.picture,
                    Topic = t.Topic,
                    UserId = t.User.Id,
                    UserName = t.User.Username,
                    IsOwner = t.User.Id == userId,
                    HasLiked = t.Likes.Any(l => l.UserId == userId),
                    TotalLikes = t.Likes.Count()
                }).ToList();

            var topLikedTemplates = _appDbContext.Templates
               .Where(t => t.DeletedAt == null && t.IsCurrent == true)
               .OrderByDescending(t => t.Likes.Count())
               .Take(5)
               .Select(t => new TemplateViewModel
               {
                   TemplateId = t.Id,
                   Title = t.Title,
                   Description = t.Description,
                   CreatedAt = t.CreatedAt,
                   UserId = t.User.Id,
                   ProfilePicture = t.User.ProfilePicture,
                   UserName = t.User.Username,
                   TotalLikes = t.Likes.Count()
               }).ToList();

            var viewModel = new HomeViewModel
            {
                Templates = templates,
                TopLikedTemplates = topLikedTemplates
            };

            return View(viewModel);
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
