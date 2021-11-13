using Microsoft.AspNetCore.Mvc;

namespace MyCourse.Controllers
{
    public class HomeController : Controller
    {
        [ResponseCache(CacheProfileName = "Home")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Benvenuto su MyCourse!";
            return View();
        }
    }
}