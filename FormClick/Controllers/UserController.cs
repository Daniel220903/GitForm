using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using FormClick.Data;
using FormClick.Models;

using System.Security.Claims;
using System;

namespace FormClick.Controllers{
    [Authorize]
    public class UserController : Controller{
        private readonly AppDBContext _appDbContext;
        private readonly FileUploadService _fileUploadService;

        public UserController(AppDBContext appDbContext, FileUploadService fileUploadService){
            _fileUploadService = fileUploadService;
            _appDbContext = appDbContext;
        }

        public IActionResult Index() {
            var userClaims = User.Identity as ClaimsIdentity;
            var userId = int.Parse(userClaims?.Claims.FirstOrDefault(c => c.Type == "Id")?.Value ?? "0");
            var user = _appDbContext.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
                return NotFound();

            // Verificar si el campo ProfilePicture es nulo o vacío
            if (string.IsNullOrEmpty(user.ProfilePicture))
            {
                user.ProfilePicture = "/uploads/profiles/default.png"; // Ruta de la imagen predeterminada
            }

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

        [HttpPost]
        public IActionResult UploadProfilePicture(IFormFile file){
            if (file != null && file.Length > 0){
                var userClaims = User.Identity as ClaimsIdentity;
                var userId = int.Parse(userClaims?.Claims.FirstOrDefault(c => c.Type == "Id")?.Value ?? "0");
                var user = _appDbContext.Users.FirstOrDefault(u => u.Id == userId);

                if (user != null){
                    var fileName = $"{user.Id}_{user.Username}_{user.CreatedAt:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");

                    if (!Directory.Exists(uploadsPath)){
                        Directory.CreateDirectory(uploadsPath);
                    }

                    var filePath = Path.Combine(uploadsPath, fileName);

                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);

                    using (var stream = new FileStream(filePath, FileMode.Create)){
                        file.CopyTo(stream);
                    }

                    user.ProfilePicture = $"/uploads/profiles/{fileName}";
                    _appDbContext.SaveChanges();

                    return Json(new { success = true, filePath = $"/uploads/profiles/{fileName}" });
                }
            }

            return Json(new { success = false, message = "No file uploaded" });
        }

    }
}
