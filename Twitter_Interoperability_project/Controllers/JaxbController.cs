using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Twitter_Interoperability_project.Controllers
{
    public class JaxbController : Controller
    {
        private readonly IHostEnvironment _hostEnvironment;

        public JaxbController(IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        [HttpPost]
        public IActionResult ValidateXml()
        {
            
            string sourcePath = Path.Combine(_hostEnvironment.ContentRootPath, "App_Data", "jobpostings.xml");
            string javaValidatorDir = @"C:\Users\pauli\OneDrive\Radna površina\Desktop\Algebra\3 godina\6.semestar\Interoperability\Jaxb\build\classes";
            string destinationPath = Path.Combine(javaValidatorDir, "jobpostings.xml");
            string xsdPath = Path.Combine(javaValidatorDir, "jobpostings.xsd");

        
            if (!System.IO.File.Exists(sourcePath))
            {
                ViewBag.JaxbValidationResult = "Error: jobpostings.xml not found in App_Data folder.";
                return View("Index");
            }
            System.IO.File.Copy(sourcePath, destinationPath, true);

          
         
            string javaExe = "java";
            string args = $"-cp . jaxb.Jaxb jobpostings.xsd jobpostings.xml";

            var process = new Process();
            process.StartInfo.FileName = javaExe;
            process.StartInfo.Arguments = args;
            process.StartInfo.WorkingDirectory = javaValidatorDir;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;

            try
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                ViewBag.JaxbValidationResult = output + (string.IsNullOrWhiteSpace(error) ? "" : "\n" + error);
            }
            catch (System.Exception ex)
            {
                ViewBag.JaxbValidationResult = "Error running Java validator: " + ex.Message;
            }

            return View("Index");
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
