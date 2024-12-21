using Microsoft.AspNetCore.Mvc;

namespace FormClick.Controllers
{
    public class CommentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
