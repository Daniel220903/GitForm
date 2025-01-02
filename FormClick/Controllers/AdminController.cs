using Microsoft.AspNetCore.Mvc;
using FormClick.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using FormClick.ViewModels;

namespace FormClick.Controllers{
    [Authorize]
    public class AdminController : Controller{
        private readonly AppDBContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDBContext context, ILogger<AdminController> logger) {
            _context = context;
            _logger = logger;
        }

        public class UserActionPayload
        {
            public string ActionType { get; set; }
            public List<string> SelectedUsers { get; set; }
        }

        public IActionResult Index() {
            var userClaims = User.Identity as ClaimsIdentity;
            var userIdClaim = userClaims?.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim)){
                TempData["Mensaje"] = "Invalid user ID.";
                return RedirectToAction("Index", "Home");
            }

            if (!int.TryParse(userIdClaim, out int userId)) {
                _logger.LogWarning("Invalid user ID format: {UserId}", userIdClaim); // Log warning
                TempData["Mensaje"] = "Invalid user ID format.";
                return RedirectToAction("Index", "Home");
            }

            var isAdmin = _context.Users.Where(u => u.Id == userId && u.DeletedAt == null).Select(u => u.Admin).FirstOrDefault();

            if (!isAdmin) {
                _logger.LogWarning("User with ID {UserId} tried to access admin section without permission.", userId); // Log warning
                TempData["Mensaje"] = "You have no access to this section.";
                return RedirectToAction("Index", "Home");
            }

            var users = _context.Users.Where(u => u.DeletedAt == null).ToList();

            var templates = _context.Templates
                //.Where(t => t.DeletedAt == null
                //            && t.IsCurrent == true
                //            && (isAdmin
                //                || t.Public
                //                || t.TemplateAccesses.Any(ta => ta.UserId == userId)
                //                || t.UserId == userId)
                //            && !_context.Responses.Any(r => r.TemplateId == t.Id && r.UserId == userId))
               .OrderByDescending(t => t.CreatedAt)
               .Select(t => new TemplateViewModel{
                   TemplateId = t.Id,
                   Title = t.Title,
                   Description = t.Description,
                   CreatedAt = t.CreatedAt,
                   DeletedAt = t.DeletedAt,
                   Version = t.Version,
                   ProfilePicture = t.User.ProfilePicture,
                   picture = t.picture,
                   Topic = t.Topic,
                   UserId = t.User.Id,
                   UserName = t.User.Username,
                   IsOwner = t.User.Id == userId,
                   IsCurrent = t.IsCurrent,
                   HasLiked = t.Likes.Any(l => l.UserId == userId),
                   TotalLikes = t.Likes.Count()
               }).ToList();

            var viewModel = new AdminVM{
                Users = users,
                Templates = templates
            };

            return View(viewModel);
        }

        [HttpPost("DeleteTemplate/{id}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var template = _context.Templates.FirstOrDefault(t => t.Id == id && t.DeletedAt == null);
            if (template == null)
                return NotFound("Template no encontrado o ya eliminado.");

            template.DeletedAt = DateTime.UtcNow;

            if (template.OriginalId != 0)
            {
                var previousTemplates = _context.Templates
                    .Where(t => t.OriginalId == template.OriginalId || t.Id == template.OriginalId && t.DeletedAt == null)
                    .ToList();

                foreach (var previousTemplate in previousTemplates)
                {
                    previousTemplate.DeletedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Template Borrado exitosamente" });
        }

        [HttpPost]
        public async Task<IActionResult> AdminAction([FromBody] UserActionPayload payload){
            //Console.
            if (payload == null || payload.SelectedUsers == null || !payload.SelectedUsers.Any()){
                _logger.LogError("No users selected or action type is missing.");
                return BadRequest("No users selected or action type is missing.");
            }

            try
            {
                switch (payload.ActionType){
                    case "borrar":
                        foreach (var userId in payload.SelectedUsers){
                            if (int.TryParse(userId, out int userIdInt)){
                                var user = await _context.Users.FindAsync(userIdInt);
                                if (user != null){
                                    user.DeletedAt = DateTime.UtcNow;
                                    _context.Users.Update(user);
                                }else{
                                    _logger.LogWarning("User not found for deletion: {UserId}", userId);
                                }
                            }else{
                                _logger.LogWarning("Invalid userId format for deletion: {UserId}", userId);
                            }
                        }
                        break;

                    case "bloquear":
                        foreach (var userId in payload.SelectedUsers){
                            if (int.TryParse(userId, out int userIdInt)) { 
                                var user = await _context.Users.FindAsync(userIdInt);
                                if (user != null){
                                    user.Banned = true;
                                    _context.Users.Update(user);
                                }else{
                                    _logger.LogWarning("User not found for blocking: {UserId}", userId);
                                }
                            }else{
                                _logger.LogWarning("Invalid userId format for blocking: {UserId}", userId);
                            }
                        }
                        break;

                    case "desbloquear":
                        foreach (var userId in payload.SelectedUsers){
                            if (int.TryParse(userId, out int userIdInt)){
                                var user = await _context.Users.FindAsync(userIdInt);
                                if (user != null){
                                    user.Banned = false;
                                    _context.Users.Update(user);
                                }else{
                                    _logger.LogWarning("User not found for unblocking: {UserId}", userId);
                                }
                            }else{
                                _logger.LogWarning("Invalid userId format for unblocking: {UserId}", userId);
                            }
                        }
                        break;

                    case "addAdmin":
                        foreach (var userId in payload.SelectedUsers){
                            if (int.TryParse(userId, out int userIdInt)){
                                var user = await _context.Users.FindAsync(userIdInt);
                                if (user != null){
                                    user.Admin = true;
                                    _context.Users.Update(user);
                                }else{
                                    _logger.LogWarning("User not found to assign admin: {UserId}", userId);
                                }
                            }else{
                                _logger.LogWarning("Invalid userId format for assigning admin: {UserId}", userId);
                            }
                        }
                        break;

                    case "deleteAdmin":
                        foreach (var userId in payload.SelectedUsers){
                            if (int.TryParse(userId, out int userIdInt)){
                                var user = await _context.Users.FindAsync(userIdInt);
                                if (user != null){
                                    user.Admin = false;
                                    _context.Users.Update(user);
                                }else{
                                    _logger.LogWarning("User not found to remove admin: {UserId}", userId);
                                }
                            }else{
                                _logger.LogWarning("Invalid userId format for removing admin: {UserId}", userId);
                            }
                        }
                        break;

                    default:
                        _logger.LogError("Invalid action type received: {ActionType}", payload.ActionType);
                        return BadRequest("Invalid action type.");
                }

                await _context.SaveChangesAsync();
                return Ok();

            }catch (Exception ex){
                _logger.LogError(ex, "Error occurred while processing user actions.");
                return StatusCode(500, "Internal server error");
            }
        }

        public IActionResult AccessDenied() {
            _logger.LogWarning("Access denied attempt to admin section.");
            return RedirectToAction("Index", "Home");
        }
    }
}
