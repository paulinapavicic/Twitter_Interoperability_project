using Microsoft.AspNetCore.Mvc;
using Twitter_Interoperability_project.Service;

namespace Twitter_Interoperability_project.Controllers
{
    public class SoapController : Controller
    {
        private readonly TwitterXmlService _twitterXmlService;

        public SoapController(TwitterXmlService twitterXmlService)
        {
            _twitterXmlService = twitterXmlService;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateXml(string query)
        {
            await _twitterXmlService.GenerateTwitterXml(query);
            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
