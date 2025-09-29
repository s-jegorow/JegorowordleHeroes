using Microsoft.AspNetCore.Mvc;

namespace JegoroWordleHeroes.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
