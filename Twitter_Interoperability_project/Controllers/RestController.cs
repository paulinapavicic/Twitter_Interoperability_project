using Microsoft.AspNetCore.Mvc;

namespace Twitter_Interoperability_project.Controllers
{
    public class RestController : Controller
    {
        public IActionResult Index()
        {
            return View("Index");
        }
    }
}
