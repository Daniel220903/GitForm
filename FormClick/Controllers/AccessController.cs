using Microsoft.AspNetCore.Mvc;

using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Security.Claims;

using FormClick.Data;
using FormClick.Models;
using FormClick.ViewModels;

using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FormClick.Controllers{
    public class AccessController : Controller {
        private readonly AppDBContext _appDbContext;

        public AccessController(AppDBContext appDbContext) {
            _appDbContext = appDbContext;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login() {
            if (User.Identity!.IsAuthenticated) { return RedirectToAction("Index", "Home"); }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model) {
            User? foundedUser = await _appDbContext.Users.Where(u => u.Email == model.Email).FirstOrDefaultAsync();

            if (foundedUser == null) {
                ViewData["Mensaje"] = "Wrong Credentials";
                return View();
            }

            var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
            var verificationResult = passwordHasher.VerifyHashedPassword(foundedUser, foundedUser.Password, model.Password);

            if (verificationResult == PasswordVerificationResult.Failed) {
                ViewData["Mensaje"] = "Wrong Credentials";
                return View();
            }

            if (!foundedUser.Verified) {
                TempData["UserId"] = foundedUser.Id;
                return RedirectToAction("Verify", "Access");
            }

            foundedUser.LastLogin = DateTime.Now;
            await _appDbContext.SaveChangesAsync();

            List<Claim> claims = new List<Claim>() {
                new Claim("Id", foundedUser.Id.ToString()),
                new Claim("Username", foundedUser.Username),
                new Claim("Admin", foundedUser.Admin.ToString()),
                new Claim("Verified", foundedUser.Verified.ToString()),
                new Claim("Banned", foundedUser.Banned.ToString()),
                new Claim("ProfilePicture", foundedUser.ProfilePicture ?? ""),
                new Claim("DarkMode", foundedUser.DarkMode.ToString()),
                new Claim("Language", foundedUser.Language ?? ""),
                new Claim(ClaimTypes.Email, foundedUser.Email)
            };

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            AuthenticationProperties properties = new AuthenticationProperties() {
                AllowRefresh = true,
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                properties
            );

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult SignUp() {
            if (User.Identity!.IsAuthenticated) { return RedirectToAction("Index", "Home"); }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(SignUpVM model) {
            User? foundedUser = await _appDbContext.Users.Where(u => u.Email == model.Email).FirstOrDefaultAsync();

            if (foundedUser != null)
            {
                ViewData["Mensaje"] = "This email is already registered.";
                return View();
            }

            if (model.Password != model.ConfirmPassword)
            {
                ViewData["Mensaje"] = "The passwords do not match.";
                return View();
            }

            DateTime currentDateTime = DateTime.Now;
            Random random = new Random();
            int randomNumber = random.Next(100000, 1000000);

            var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();

            User user = new User {
                Username = model.Username,
                Name = model.Name,
                Cellphone = "0",
                Email = model.Email,
                Password = passwordHasher.HashPassword(null, model.Password),
                ProfilePicture = "",
                Admin = false,
                Verified = false,
                VerifiedCode = randomNumber,
                Banned = false,
                LastLogin = null,
                Language = model.Language,
                CreatedAt = currentDateTime,
                UpdatedAt = currentDateTime,
                DeletedAt = null
            };

            await _appDbContext.Users.AddAsync(user);
            await _appDbContext.SaveChangesAsync();

            if (user.Id != 0) {
                try {
                    SendVerificationEmail(user.Email, user.VerifiedCode);
                    TempData["Mensaje"] = "User Created Successfully. A verification email has been sent.";
                    return RedirectToAction("Login", "Access");
                } catch (Exception ex) {
                    ViewData["Mensaje"] = $"User created but failed to send verification email: {ex.Message}";
                    return View();
                }
            }

            ViewData["Mensaje"] = "User could not be created, error";
            return View();
        }

        [HttpGet]
        public IActionResult Verify()
        {
            if (TempData["UserId"] == null)
                return RedirectToAction("Login", "Access");

            ViewData["UserId"] = TempData["UserId"];
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Verify(int userId, int verificationCode)
        {
            User? user = await _appDbContext.Users.FindAsync(userId);

            if (user == null || user.VerifiedCode != verificationCode) {
                ViewData["Mensaje"] = "Invalid verification code.";
                ViewData["UserId"] = userId;
                return View();
            }

            user.Verified = true;
            user.LastLogin = DateTime.Now;
            await _appDbContext.SaveChangesAsync();

            List<Claim> claims = new List<Claim>() {
                new Claim("Id", user.Id.ToString()),
                new Claim("Username", user.Username),
                new Claim("Admin", user.Admin.ToString()),
                new Claim("Verified", user.Verified.ToString()),
                new Claim("Banned", user.Banned.ToString()),
                new Claim("ProfilePicture", user.ProfilePicture ?? ""),
                new Claim("DarkMode", user.DarkMode.ToString()),
                new Claim("Language", user.Language ?? ""),
                new Claim(ClaimTypes.Email, user.Email)
            };

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            AuthenticationProperties properties = new AuthenticationProperties()
            {
                AllowRefresh = true,
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                properties
            );

            return RedirectToAction("Index", "Home");
        }


        private void SendVerificationEmail(string toEmail, int verificationCode){
            var smtpSettings = new {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Username = "danieldev0309@gmail.com",
                Password = "fubk qmhp lvac iqda"
            };

            using (var client = new SmtpClient(smtpSettings.Host, smtpSettings.Port)){
                client.Credentials = new NetworkCredential(smtpSettings.Username, smtpSettings.Password);
                client.EnableSsl = smtpSettings.EnableSsl;

                var mailMessage = new MailMessage{
                    From = new MailAddress(smtpSettings.Username),
                    Subject = "Hi thanks for signing up heres your Verification Code",
                    Body = $"Your verification code is: {verificationCode}",
                    IsBodyHtml = false
                };

                mailMessage.To.Add(toEmail);
                client.Send(mailMessage);
            }
        }

        public async Task<IActionResult> Logout(){
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Access");
        }
    }
}
