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
            var soapResponse = await response.Content.ReadAsStringAsync();

            // <--- ADD THIS LINE HERE
            System.IO.File.WriteAllText("App_Data/last_soap_response.xml", soapResponse);

           
            var xdoc = XDocument.Parse(soapResponse);
            var searchResultsNode = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "SearchJobPostingsResult");

            var jobPostings = searchResultsNode?
                .Elements()
                .Where(e => e.Name.LocalName == "JobPosting")
                .Select(e => new
                {
                    Title = e.Element("Title")?.Value,
                    CompanyName = e.Element("CompanyName")?.Value,
                    Location = e.Element("Location")?.Value,
                    JobDescription = e.Element("JobDescription")?.Value
                })
                .ToList();

            ViewBag.JobPostings = jobPostings;
            ViewBag.SearchTerm = term;
            ViewBag.SoapResultsXml = searchResultsNode != null ? searchResultsNode.ToString() : "No results found or error in SOAP response.";

            return View("Index");
        }



        [HttpPost]
        public async Task<IActionResult> GenerateXml(string query)
        {
            await _twitterXmlService.GenerateJobPostingsXml(query);
            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
