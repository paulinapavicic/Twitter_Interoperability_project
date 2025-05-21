using Microsoft.AspNetCore.Mvc;
using Twitter_Interoperability_project.Models;
using Twitter_Interoperability_project.Service;

namespace Twitter_Interoperability_project.Controllers
{
    public class XsdController : Controller
    {
        private readonly XmlValidationService _validator = new XmlValidationService();

        [HttpGet]
        public IActionResult Index()
        {
            return View(new XsdValidationViewModel());
        }

        [HttpPost]
        public IActionResult Index(XsdValidationViewModel model)
        {
            var errors = _validator.ValidateXmlWithXsdString(model.XmlData, model.XsdData);

            if (errors.Count == 0)
                model.Result = "<span style='color:green;'>XML is valid according to XSD!</span>";
            else
                model.Result = "<span style='color:red;'>Validation errors:<br/>" + string.Join("<br/>", errors) + "</span>";

            return View(model);
        }
    }
}
