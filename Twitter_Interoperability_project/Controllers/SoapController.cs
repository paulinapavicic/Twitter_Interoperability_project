using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Xml.Linq;
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
        public async Task<IActionResult> SearchSoap(string term)
        {
            try
            {
                var soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <SearchJobPostings xmlns=""http://tempuri.org/"">
      <term>{System.Net.WebUtility.HtmlEncode(term)}</term>
    </SearchJobPostings>
  </soap:Body>
</soap:Envelope>";

                using var client = new HttpClient();
                var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                content.Headers.Add("SOAPAction", "\"http://tempuri.org/IJobPostingSoapService/SearchJobPostings\"");

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var response = await client.PostAsync($"{baseUrl}/JobPostingSoap.svc", content);

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.SoapError = $"SOAP service returned status code: {response.StatusCode}";
                    return View("Index");
                }

                var soapResponse = await response.Content.ReadAsStringAsync();

                
                try
                {
                    Directory.CreateDirectory("App_Data");
                    System.IO.File.WriteAllText("App_Data/last_soap_response.xml", soapResponse);
                }
                catch (Exception ex)
                {
                    ViewBag.SoapError = "Could not save SOAP response for debugging: " + ex.Message;
                }

              
                XDocument xdoc;
                try
                {
                    xdoc = XDocument.Parse(soapResponse);
                }
                catch (System.Xml.XmlException ex)
                {
                    ViewBag.SoapError = "Malformed SOAP response: " + ex.Message;
                    return View("Index");
                }

                var searchResultsNode = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "SearchJobPostingsResult");

                if (searchResultsNode == null)
                {
                    ViewBag.SoapError = "No search results found in SOAP response.";
                    return View("Index");
                }

               
                var innerXml = searchResultsNode.Value ?? searchResultsNode.FirstNode?.ToString();
                var unescapedXml = System.Net.WebUtility.HtmlDecode(innerXml);
                var innerDoc = XDocument.Parse(unescapedXml);

                var jobPostings = innerDoc
                    .Descendants()
                    .Where(e => e.Name.LocalName == "JobPosting")
                    .Select(e => new
                    {
                        Title = e.Elements().FirstOrDefault(el => el.Name.LocalName == "Title")?.Value,
                        CompanyName = e.Elements().FirstOrDefault(el => el.Name.LocalName == "CompanyName")?.Value,
                        Location = e.Elements().FirstOrDefault(el => el.Name.LocalName == "Location")?.Value,
                        JobDescription = e.Elements().FirstOrDefault(el => el.Name.LocalName == "JobDescription")?.Value
                    })
                    .ToList();

                ViewBag.JobPostings = jobPostings;
                ViewBag.SearchTerm = term;
                ViewBag.SoapResultsXml = unescapedXml;

                if (jobPostings.Any())
                    ViewBag.SoapMessage = $"Search completed successfully! {jobPostings.Count} result(s) found.";
                else
                    ViewBag.SoapError = "No job postings matched the given term.";

                return View("Index");
            }
            catch (HttpRequestException ex)
            {
                ViewBag.SoapError = "Could not reach the SOAP service. Details: " + ex.Message;
                return View("Index");
            }
            catch (FileNotFoundException ex)
            {
                ViewBag.SoapError = "Data file not found. Please generate the XML file first.";
                return View("Index");
            }
            catch (System.Xml.XmlException ex)
            {
                ViewBag.SoapError = "Malformed XML: " + ex.Message;
                return View("Index");
            }
            catch (Exception ex)
            {
                ViewBag.SoapError = "An unexpected error occurred: " + ex.Message;
                return View("Index");
            }
        }

      



        [HttpPost]
        public async Task<IActionResult> GenerateXml(string query)
        {
            try
            {
                await _twitterXmlService.GenerateJobPostingsXml(query);
                TempData["XmlMessage"] = "XML generated successfully!";
            }
            catch (ArgumentException ex)
            {
                TempData["XmlError"] = "Invalid query: " + ex.Message;
            }
            catch (Exception ex)
            {
                TempData["XmlError"] = "An error occurred while generating XML: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
