using CookComputing.XmlRpc;
using Microsoft.AspNetCore.Mvc;
using Twitter_Interoperability_project.Interfaces;
using Twitter_Interoperability_project.Models;

namespace Twitter_Interoperability_project.Controllers
{
    public class RpcController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string cityPart)
        {
            CityTemp[] results = null;
            string error = null;
            try
            {
                var proxy = XmlRpcProxyGen.Create<IWeatherService>();
                proxy.Url = "http://localhost:8080/";
                results = proxy.GetTemperature(cityPart);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            ViewBag.Results = results;
            ViewBag.CityPart = cityPart;
            ViewBag.Error = error;
            return View();
        }
    }
}
