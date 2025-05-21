using Microsoft.AspNetCore.Mvc;
using Twitter_Interoperability_project.Models;
using Twitter_Interoperability_project.Service;

namespace Twitter_Interoperability_project.Controllers
{
    public class RngController : Controller
    {
        private readonly RngValidationService _validator = new RngValidationService();

        // GET: /Rng/Index
        [HttpGet]
        public IActionResult Index()
        {
            // Initialize a new model for GET requests
            var model = new RngValidationViewModel
            {
                XmlData = "",     // Default empty XML
                RngData = "",     // Default empty RNG schema
                Result = null
            };
            return View(model);
        }

        // POST: /Rng/Index
        [HttpPost]
        public IActionResult Index(RngValidationViewModel model)
        {
            if (model == null)
            {
                // Handle null model (unlikely, but defensive coding)
                model = new RngValidationViewModel();
            }

            var errors = _validator.ValidateXmlWithRngString(model.XmlData, model.RngData);

            if (errors.Count == 0)
                model.Result = "<span style='color:green;'>XML is valid according to RNG!</span>";
            else
                model.Result = "<span style='color:red;'>Validation errors:<br/>" + string.Join("<br/>", errors) + "</span>";

            // Return the same view with the updated model
            return View(model);
        }
    }
}
