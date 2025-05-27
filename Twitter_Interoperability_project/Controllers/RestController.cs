using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Twitter_Interoperability_project.Controllers
{
    public class RestController : Controller
    {
        private readonly string apiUrl = "https://twitter241.p.rapidapi.com/search/";
        private readonly string apiKey = "f0c3be805emsh767cbcd3c6c4040p18cb0ejsndcda90a0978b"; 
        private readonly string apiHost = "twitter241.p.rapidapi.com";
        // Local API
        private readonly string localApiUrl = "https://localhost:5001/api/jobpostings"; // Change port if needed

        // GET/search (Twitter API)
        [HttpPost]
        public async Task<IActionResult> GetBySkill(string skill)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-RapidAPI-Key", apiKey);
                client.DefaultRequestHeaders.Add("X-RapidAPI-Host", apiHost);

                var url = $"{apiUrl}?query={System.Net.WebUtility.UrlEncode(skill)}&type=Latest&count=20";
                var response = await client.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                // Parse the timeline as in GenerateJobPostingsXml
                var jObj = JObject.Parse(json);
                var entries = jObj["result"]?["timeline"]?["instructions"]?[0]?["entries"];
                var jobList = new StringBuilder();

                if (entries != null)
                {
                    foreach (var e in entries)
                    {
                        var tr = e["content"]?["itemContent"]?["tweet_results"]?["result"];
                        if (tr != null)
                        {
                            var tweet = tr["tweet"] ?? tr;
                            var user = tweet["core"]?["user_results"]?["result"]?["legacy"];
                            var username = user?["screen_name"]?.ToString();
                            var author = user?["name"]?.ToString();
                            var legacy = tweet["legacy"];
                            jobList.AppendLine($"Title: {(legacy?["full_text"] ?? legacy?["text"])}");
                            jobList.AppendLine($"Company: {author}");
                            jobList.AppendLine($"Location: {user?["location"] ?? ""}");
                            jobList.AppendLine($"Job page: https://x.com/{username}/status/{tweet["rest_id"]}");
                            jobList.AppendLine("-----");
                        }
                    }
                }
                else
                {
                    jobList.AppendLine("No job data found.");
                }

                ViewBag.JobInfo = jobList.ToString();
                ViewBag.RestResult = PrettyPrintJson(json);
            }
            return View("Index");
        }


        private string ExtractJobInfo(string json)
        {
            var job = JObject.Parse(json)["result"]?["jobData"]?["result"];
            if (job == null) return "No job data found.";

            var core = job["core"];
            var company = job["company_profile_results"]?["result"]?["core"]?["name"];
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Title: {core?["title"]}");
            sb.AppendLine($"Company: {company}");
            sb.AppendLine($"Location: {core?["location"]}");
            sb.AppendLine($"Seniority: {core?["seniority_level"]}");
            sb.AppendLine($"Job page: {core?["job_page_url"]}");
            return sb.ToString();
        }

        private string PrettyPrintJson(string json)
        {
            try
            {
                var parsed = JToken.Parse(json);
                return parsed.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }

        // POST (create local job)
        [HttpPost]
        public async Task<IActionResult> PostJob(string jsonData)
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(localApiUrl, content);
                var result = await response.Content.ReadAsStringAsync();
                ViewBag.LocalApiResult = result;
            }
            return View("Index");
        }

        // PUT (update local job)
        [HttpPost]
        public async Task<IActionResult> PutJob(string id, string jsonData)
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PutAsync($"{localApiUrl}/{id}", content);
                var result = await response.Content.ReadAsStringAsync();
                ViewBag.LocalApiResult = result;
            }
            return View("Index");
        }

        // DELETE (delete local job)
        [HttpPost]
        public async Task<IActionResult> DeleteJob(string id)
        {
            using (var client = new HttpClient())
            {
                var response = await client.DeleteAsync($"{localApiUrl}/{id}");
                var result = await response.Content.ReadAsStringAsync();
                ViewBag.LocalApiResult = result;
            }
            return View("Index");
        }

   

        public IActionResult Index()
        {
            return View();
        }
    }
}
