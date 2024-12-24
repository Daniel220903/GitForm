using Microsoft.AspNetCore.Mvc;
using FormClick.Data;
using FormClick.Models;
using System.Linq;

namespace FormClick.Controllers
{
    public class CommentController : Controller
    {
        private readonly AppDBContext _appDbContext;

        public CommentController(AppDBContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public class CommentRequest {
            public int TemplateId { get; set; }
            public string CommentText { get; set; }
        }


        [HttpPost]
        public IActionResult Add([FromBody] CommentRequest request) {
            var claimsIdentity = User.Identity as System.Security.Claims.ClaimsIdentity;
            var userIdClaim = claimsIdentity?.FindFirst("Id")?.Value;
            int userId = int.TryParse(userIdClaim, out var idParsed) ? idParsed : 0;

            if (string.IsNullOrWhiteSpace(request.CommentText))
                return Json(new { success = false, message = "El comentario no puede estar vacío." });

            var newComment = new Comment {
                TemplateId = request.TemplateId,
                UserId = userId,
                CommentText = request.CommentText,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _appDbContext.Comments.Add(newComment);
            _appDbContext.SaveChanges();

            return Json(new { success = true, message = "Comentario agregado exitosamente." });
        }

        [HttpGet]
        public IActionResult GetComments(int templateId)
        {
            var comments = _appDbContext.Comments
                .Where(c => c.TemplateId == templateId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CommentText,
                    CreatedAt = c.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    UserName = c.User.Username,
                    ProfilePicture = c.User.ProfilePicture
                })
                .ToList();

            Console.Write(comments);
            return Json(comments);
        }
    }
}
