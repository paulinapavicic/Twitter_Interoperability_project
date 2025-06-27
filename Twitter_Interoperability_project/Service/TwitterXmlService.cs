using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace Twitter_Interoperability_project.Service
{
    public class TwitterXmlService
    {
        private readonly IConfiguration _config;
        private const string XmlFilePath = "App_Data/jobpostings.xml";

        public TwitterXmlService(IConfiguration config)
        {
            _config = config;
        }

        public async Task GenerateJobPostingsXml(string query)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-RapidAPI-Key", _config["Twitter:ApiKey"]);
            client.DefaultRequestHeaders.Add("X-RapidAPI-Host", "twitter241.p.rapidapi.com");

            var response = await client.GetAsync(
     $"https://twitter241.p.rapidapi.com/jobs-search?keyword={Uri.EscapeDataString(query)}&count=20");


            var json = await response.Content.ReadAsStringAsync();
            System.IO.File.WriteAllText("App_Data/last_api_response.json", json);

            var jObj = JObject.Parse(json);
            var items = jObj["result"]?["job_search"]?["items_results"] as JArray;

            if (items == null || !items.Any())
            {
                Directory.CreateDirectory("App_Data");
                new XElement("JobPostings").Save("App_Data/jobpostings.xml");
                return;
            }

            var xml = new XElement("JobPostings",
                items.Select(item =>
                {
                    var job = item["result"];
                    var core = job?["core"];
                    var company = job?["company_profile_results"]?["result"];
                    var companyCore = company?["core"];
                    var companyLogo = company?["logo"];
                    var user = job?["user_results"]?["result"];
                    var legacy = user?["legacy"];

                    return new XElement("JobPosting",
                        new XElement("Id", item["id"]?.ToString() ?? ""),
                        new XElement("RestId", item["rest_id"]?.ToString() ?? ""),
                        new XElement("Title", core?["title"]?.ToString() ?? ""),
                        new XElement("ExternalUrl", core?["redirect_url"]?.ToString() ?? ""),
                        new XElement("JobPageUrl", core?["redirect_url"]?.ToString() ?? ""),
                        new XElement("Location", core?["location"]?.ToString() ?? ""),
                        new XElement("CompanyName", companyCore?["name"]?.ToString() ?? legacy?["name"]?.ToString() ?? ""),
                        new XElement("CompanyLogoUrl", companyLogo?["normal_url"]?.ToString() ?? legacy?["profile_image_url_https"]?.ToString() ?? "")
                    );
                })
            );

            Directory.CreateDirectory("App_Data");
            xml.Save(XmlFilePath);

        }
    }
}
