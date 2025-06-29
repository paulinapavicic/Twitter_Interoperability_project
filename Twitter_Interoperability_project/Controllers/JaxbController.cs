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

            string appDataPath = Path.Combine(_hostEnvironment.ContentRootPath, "App_Data");
            string sourceXmlPath = Path.Combine(appDataPath, "jobpostings.xml");
            string sourceXsdPath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "schema", "jobpostings.xsd");
            string javaValidatorDir = @"C:\Users\pauli\OneDrive\Radna površina\Desktop\Paulina_Pavičić_Interoperability\Jaxb\build\classes";
            string destXmlPath = Path.Combine(javaValidatorDir, "jobpostings.xml");
            string destXsdPath = Path.Combine(javaValidatorDir, "jobpostings.xsd");

            if (!System.IO.File.Exists(sourceXmlPath))
            {
                ViewBag.JaxbValidationResult = "Error: jobpostings.xml not found in App_Data folder.";
                return View("Index");
            }
            if (!System.IO.File.Exists(sourceXsdPath))
            {
                ViewBag.JaxbValidationResult = "Error: jobpostings.xsd not found in schema folder.";
                return View("Index");
            }

            try
            {
                System.IO.File.Copy(sourceXmlPath, destXmlPath, true);
                System.IO.File.Copy(sourceXsdPath, destXsdPath, true);
            }
            catch (Exception ex)
            {
                ViewBag.JaxbValidationResult = "Error copying files: " + ex.Message;
                return View("Index");
            }

            string javaExe = "java";
            string javaClass = "jaxb.Jaxb";

            var process = new Process();
            process.StartInfo.FileName = javaExe;
            process.StartInfo.WorkingDirectory = javaValidatorDir;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            
            process.StartInfo.ArgumentList.Add("-cp");
            process.StartInfo.ArgumentList.Add(".");
            process.StartInfo.ArgumentList.Add(javaClass);
            process.StartInfo.ArgumentList.Add("jobpostings.xsd");
            process.StartInfo.ArgumentList.Add("jobpostings.xml");

            try
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    ViewBag.JaxbValidationResult = "Java process failed with exit code " + process.ExitCode +
                        "\nOutput:\n" + output +
                        (string.IsNullOrWhiteSpace(error) ? "" : "\nErrors:\n" + error);
                }
                else
                {
                    ViewBag.JaxbValidationResult = output +
                        (string.IsNullOrWhiteSpace(error) ? "" : "\nErrors:\n" + error);
                }
            }
            catch (Exception ex)
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
