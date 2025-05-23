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
                $"https://twitter241.p.rapidapi.com/search?query={Uri.EscapeDataString(query)}&type=Latest&count=20");

            var json = await response.Content.ReadAsStringAsync();
            System.IO.File.WriteAllText("App_Data/last_api_response.json", json); // For debugging

            var jObj = JObject.Parse(json);

           
            var entries = jObj["result"]?["timeline"]?["instructions"]?[0]?["entries"];
            if (entries == null)
            {
                Directory.CreateDirectory("App_Data");
                new XElement("JobPostings").Save(XmlFilePath);
                return;
            }

            var xml = new XElement("JobPostings",
                entries
                    .Select(e => e["content"]?["itemContent"]?["tweet_results"]?["result"])
                    .Where(tr => tr != null)
                    .Select(tr =>
                    {
                       
                        var tweet = tr["tweet"] ?? tr;

                        
                        var user = tweet["core"]?["user_results"]?["result"]?["legacy"];
                        var username = user?["screen_name"]?.ToString();
                        var author = user?["name"]?.ToString();

                        
                        var legacy = tweet["legacy"];
                        return new XElement("JobPosting",
                            new XElement("Id", tweet["rest_id"]?.ToString()),
                            new XElement("Title", (legacy?["full_text"] ?? legacy?["text"])?.ToString()),
                            new XElement("CompanyName", author),
                            new XElement("Location", user?["location"]?.ToString() ?? ""),
                            new XElement("JobDescription", legacy?["full_text"]?.ToString() ?? legacy?["text"]?.ToString()),
                            new XElement("JobPageUrl", $"https://x.com/{username}/status/{tweet["rest_id"]}"),
                            new XElement("ExternalUrl", $"https://x.com/{username}/status/{tweet["rest_id"]}"),
                            new XElement("CompanyLogoUrl", user?["profile_image_url_https"]?.ToString() ?? "")
                        );
                    })
            );

            Directory.CreateDirectory("App_Data");
            xml.Save(XmlFilePath);

        }
    }
}
