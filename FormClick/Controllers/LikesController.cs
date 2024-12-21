using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using FormClick.Data;
using FormClick.Models;

using System.Security.Claims;
using System;

namespace FormClick.Controllers
{
    [Authorize]
    public class LikesController : Controller
    {
        private readonly AppDBContext _appDbContext;
        private readonly FileUploadService _fileUploadService;

        public LikesController(AppDBContext appDbContext, FileUploadService fileUploadService)
        {
            _fileUploadService = fileUploadService;
            _appDbContext = appDbContext;
        }

        [HttpPost("Likes/ToggleLike/{templateId}")]
        public async Task<IActionResult> ToggleLike(int templateId)
        {
            var userClaims = User.Identity as ClaimsIdentity;
            var userIdClaim = userClaims?.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var like = _appDbContext.Likes.FirstOrDefault(l => l.TemplateId == templateId && l.UserId == userId);

            if (like != null)
            {
                _appDbContext.Likes.Remove(like);
            }
            else
            {
                _appDbContext.Likes.Add(new Like
                {
                    TemplateId = templateId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _appDbContext.SaveChanges();

            var totalLikes = _appDbContext.Likes.Count(l => l.TemplateId == templateId);

            return Ok(new { totalLikes });
        }

    }
}
