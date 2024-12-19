using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormClick.Data;
using FormClick.Models;
using System.Security.Claims;
using System;

namespace FormClick.Controllers{
    [Authorize]
    public class UserController : Controller{
        private readonly AppDBContext _appDbContext;

        public UserController(AppDBContext appDbContext){
            _appDbContext = appDbContext;
        }

        public IActionResult Index(){
            var userClaims = User.Identity as ClaimsIdentity;
            var userId = int.Parse(userClaims?.Claims.FirstOrDefault(c => c.Type == "Id")?.Value ?? "0");
            var user = _appDbContext.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        public IActionResult Update(User model){
            var userClaims = User.Identity as ClaimsIdentity;
            var userId = int.Parse(userClaims?.Claims.FirstOrDefault(c => c.Type == "Id")?.Value ?? "0");
            var user = _appDbContext.Users.FirstOrDefault(u => u.Id == userId);

            if (user != null){
                user.Name = model.Name;
                user.Cellphone = model.Cellphone;
                user.Language = model.Language;
                user.DarkMode = model.DarkMode;

                _appDbContext.SaveChanges();
            }
            
            return RedirectToAction("Index", "Home");
        }

    }
}
