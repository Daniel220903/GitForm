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
        public IActionResult Home(){

            var templates = _appDbContext.Templates
                .Where(t => t.DeletedAt == null && t.IsCurrent == true)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TemplateViewModel {
                    TemplateId = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Version = t.Version,
                    CreatedAt = t.CreatedAt,
                    ProfilePicture = t.User.ProfilePicture,
                    picture = t.picture,
                    Topic = t.Topic,
                    UserId = t.User.Id,
                    UserName = t.User.Username,
                    TotalLikes = t.Likes.Count()
                }).ToList();

            var topLikedTemplates = _appDbContext.Templates
                .Where(t => t.DeletedAt == null && t.IsCurrent == true)
                .OrderByDescending(t => t.Likes.Count())
                .Take(5)
                .Select(t => new TemplateViewModel {
                    TemplateId = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    UserId = t.User.Id,
                    ProfilePicture = t.User.ProfilePicture,
                    UserName = t.User.Username,
                    TotalLikes = t.Likes.Count()
                }).ToList();

            var viewModel = new HomeViewModel {
                Templates = templates,
                TopLikedTemplates = topLikedTemplates
            };

            return View(viewModel);
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
        public IActionResult Verify() {
            if (TempData["UserId"] == null)
                return RedirectToAction("Login", "Access");

            ViewData["UserId"] = TempData["UserId"];
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Verify(int userId, int verificationCode) {
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

        public class SearchRequest{
            public string SearchTerm { get; set; }
        }

        [HttpPost]
        public IActionResult Search([FromBody] SearchRequest request) {
            try {
                Console.WriteLine("Search Term: " + request.SearchTerm);

                var templates = _appDbContext.Templates
                    .Where(t => t.DeletedAt == null && t.IsCurrent == true &&
                        (t.Title.Contains(request.SearchTerm) ||
                         t.Description.Contains(request.SearchTerm) ||
                         t.Topic.Contains(request.SearchTerm) ||
                         t.User.Username.Contains(request.SearchTerm) ||
                         t.User.Email.Contains(request.SearchTerm)))
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(30)
                    .Select(t => new TemplateViewModel {
                        TemplateId = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        Version = t.Version,
                        CreatedAt = t.CreatedAt,
                        ProfilePicture = t.User.ProfilePicture,
                        Topic = t.Topic,
                        UserId = t.User.Id,
                        UserName = t.User.Username,
                        TotalLikes = t.Likes.Count()
                    }).ToList();
                Console.Write(templates);
                return Json(templates);
            } catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpPost]
        public IActionResult LoggedSearch([FromBody] SearchRequest request) {
            var userClaims = User.Identity as ClaimsIdentity;
            var userIdClaim = userClaims?.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            var userId = int.Parse(userIdClaim);

            var isAdmin = _appDbContext.Users.Where(u => u.Id == userId).Select(u => u.Admin).FirstOrDefault();

            var templates = _appDbContext.Templates
                .Where(t => t.DeletedAt == null && t.IsCurrent == true &&
                    (t.Title.Contains(request.SearchTerm) ||
                        t.Description.Contains(request.SearchTerm) ||
                        t.Topic.Contains(request.SearchTerm) ||
                        t.User.Username.Contains(request.SearchTerm) ||
                        t.User.Email.Contains(request.SearchTerm)) &&
                    (isAdmin
                        || t.Public
                        || t.TemplateAccesses.Any(ta => ta.UserId == userId)
                        || t.UserId == userId) 
                        && !_appDbContext.Responses.Any(r => r.TemplateId == t.Id && r.UserId == userId)
                ).OrderByDescending(t => t.CreatedAt).Take(30)
                .Select(t => new TemplateViewModel {
                    TemplateId = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    Version = t.Version,
                    ProfilePicture = t.User.ProfilePicture,
                    Topic = t.Topic,
                    UserId = t.User.Id,
                    UserName = t.User.Username,
                    TotalLikes = t.Likes.Count()
                }).ToList();

            return Json(templates);
        }

        [HttpPost]
        public async Task<IActionResult> LoggedSearchWithResponses([FromBody] SearchRequest request){
            var userClaims = User.Identity as ClaimsIdentity;
            var userIdClaim = userClaims?.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            var userId = int.Parse(userIdClaim);

            var isAdmin = _appDbContext.Users.Where(u => u.Id == userId).Select(u => u.Admin).FirstOrDefault();

            try {
                var templates = await _appDbContext.Templates
                    .Where(t => t.UserId == userId)
                    .Where(t => t.DeletedAt == null && t.IsCurrent == true &&
                        (t.Title.Contains(request.SearchTerm) ||
                            t.Description.Contains(request.SearchTerm) ||
                            t.Topic.Contains(request.SearchTerm) ||
                            t.User.Username.Contains(request.SearchTerm) ||
                            t.User.Email.Contains(request.SearchTerm)) &&
                        (isAdmin
                            || t.Public
                            || t.TemplateAccesses.Any(ta => ta.UserId == userId)
                            || t.UserId == userId)
                    )
                    .OrderByDescending(t => t.CreatedAt)
                    .Include(t => t.Responses)
                    .ThenInclude(r => r.User)
                    .Include(t => t.User)
                    .ToListAsync();

                var answerTemplateVMs = templates.Select(t => new AnswerTemplateVM{
                    TemplateId = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    UserId = userId,
                    UserName = t.User?.Username ?? "Unknown",
                    ProfilePicture = t.User?.ProfilePicture ?? "default_picture.jpg",
                    picture = t.picture,
                    Topic = t.Topic,
                    TotalLikes = t.Likes?.Count() ?? 0,
                    Responses = t.Responses?.Select(r => new ResponsedBYVM {
                        ResponseId = r.Id,
                        UserId = r.User?.Id ?? 0,
                        UserName = r.User?.Username ?? "Unknown",
                        ProfilePicture = r.User?.ProfilePicture ?? "default_profile_picture.jpg",
                        Score = r.Score,
                        CreatedAt = r.CreatedAt
                    }).ToList() ?? new List<ResponsedBYVM>()
                }).ToList();

                Console.WriteLine($"Found {answerTemplateVMs.Count} answer templates.");

                return Json(answerTemplateVMs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return Json(new { error = "An error occurred", details = ex.Message });
            }
        }


    }
}
