using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace Twitter_Interoperability_project.Service
{
    public class TwitterXmlService
    {
        private readonly IConfiguration _config;
        private const string XmlFilePath = "App_Data/tweets.xml";

        public TwitterXmlService(IConfiguration config)
        {
            _config = config;
        }

        public async Task GenerateTwitterXml(string query)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-RapidAPI-Key", _config["Twitter:ApiKey"]);
            client.DefaultRequestHeaders.Add("X-RapidAPI-Host", "twitter241.p.rapidapi.com");

            var response = await client.GetAsync(
                $"https://twitter241.p.rapidapi.com/search?query={Uri.EscapeDataString(query)}&type=Latest&count=20");

            var json = await response.Content.ReadAsStringAsync();
            var tweets = JObject.Parse(json)["data"];

            var xml = new XElement("Tweets",
                tweets?.Select(t => new XElement("Tweet",
                    new XElement("Id", t["id"]?.ToString()),
                    new XElement("Text", t["text"]?.ToString()),
                    new XElement("Author", t["user"]?["username"]?.ToString()),
                    new XElement("CreatedAt", t["created_at"]?.ToString())
                ))
            );

            Directory.CreateDirectory("App_Data");
            xml.Save(XmlFilePath);
        }
    }
}
