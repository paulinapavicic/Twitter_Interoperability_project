using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using Twitter_Interoperability_project.Models;


namespace Twitter_Interoperability_project.Controllers
{
    public class RestController : Controller
    {
        
        private readonly string apiUrl = "https://twitter241.p.rapidapi.com/search/";
        private readonly string apiKey = "f0c3be805emsh767cbcd3c6c4040p18cb0ejsndcda90a0978b";
        private readonly string apiHost = "twitter241.p.rapidapi.com";

        
        private readonly string localApiUrl = "https://localhost:7186/api/twitterusers";

        // Use absolute path for data file
        private readonly string dataPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "App_Data", "twitterusers.json"
        );

        [HttpPost]
        public async Task<IActionResult> ImportJsonFromApi()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-RapidAPI-Key", apiKey);
                    client.DefaultRequestHeaders.Add("X-RapidAPI-Host", apiHost);

                    var url = $"{apiUrl}?query=developer&type=Latest&count=20";
                    var response = await client.GetAsync(url);
                    var json = await response.Content.ReadAsStringAsync();

                    // Save raw API response for debugging
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data"));
                    System.IO.File.WriteAllText(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "last_api_response.json"),
                        json
                    );

                    // Parse users from API response
                    var apiUsers = ParseTwitterApiResponse(json);

                    ViewBag.JobInfo = $"Parsed {apiUsers.Count} users from API. ";

                    // Import users into your local JSON file via API
                    var importResponse = await client.PostAsync(
                        $"{localApiUrl}/import",
                        new StringContent(JsonConvert.SerializeObject(apiUsers), Encoding.UTF8, "application/json")
                    );
                        
                    var importResult = await importResponse.Content.ReadAsStringAsync();

                    if (importResponse.IsSuccessStatusCode && apiUsers.Count > 0)
                    {
                        TempData["JsonImported"] = true;
                        ViewBag.JobInfo += "Import successful. CRUD is now enabled.";
                    }
                    else
                    {
                        TempData["JsonImported"] = false;
                        ViewBag.JobInfo += $"Import failed! {importResult}";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["JsonImported"] = false;
                ViewBag.JobInfo = $"Error: {ex.Message}";
            }
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> SearchUserByUsername(string username)
        {
            if (!IsJsonImported())
            {
                ViewBag.JobInfo = "You must import JSON from API first!";
                return View("Index");
            }
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{localApiUrl}/search/{username}");
                var json = await response.Content.ReadAsStringAsync();
                ViewBag.SearchUserResult = PrettyPrintJson(json);
            }
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(string jsonData)
        {
            if (!IsJsonImported())
            {
                ViewBag.JobInfo = "You must import JSON from API first!";
                return View("Index");
            }
            using (var client = new HttpClient())
            {
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(localApiUrl, content);
                var result = await response.Content.ReadAsStringAsync();
                ViewBag.LocalApiResult = result;
            }
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser(string id, string jsonData)
        {
            if (!IsJsonImported())
            {
                ViewBag.JobInfo = "You must import JSON from API first!";
                return View("Index");
            }
            using (var client = new HttpClient())
            {
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PutAsync($"{localApiUrl}/{id}", content);
                var result = await response.Content.ReadAsStringAsync();
                ViewBag.LocalApiResult = result;
            }
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (!IsJsonImported())
            {
                ViewBag.JobInfo = "You must import JSON from API first!";
                return View("Index");
            }
            using (var client = new HttpClient())
            {
                var response = await client.DeleteAsync($"{localApiUrl}/{id}");
                var result = await response.Content.ReadAsStringAsync();
                ViewBag.LocalApiResult = result;
            }
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> GetAllUsers()
        {
            if (!IsJsonImported())
            {
                ViewBag.JobInfo = "You must import JSON from API first!";
                return View("Index");
            }
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(localApiUrl);
                var json = await response.Content.ReadAsStringAsync();
                ViewBag.LocalApiAll = PrettyPrintJson(json);
            }
            return View("Index");
        }

        private bool IsJsonImported()
        {
            return System.IO.File.Exists(dataPath) && new System.IO.FileInfo(dataPath).Length > 0;
        }

        private string PrettyPrintJson(string json)
        {
            try
            {
                var parsed = JToken.Parse(json);
                return parsed.ToString(Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }

        // Parse users from Twitter API response (deeply nested structure)
        private List<TwitterUser> ParseTwitterApiResponse(string json)
        {
            var apiUsers = new List<TwitterUser>();
            try
            {
                var jObj = JObject.Parse(json);
                // Try the "results" array (for some APIs)
                var results = jObj["results"];
                if (results != null)
                {
                    foreach (var item in results)
                    {
                        apiUsers.Add(new TwitterUser
                        {
                            user_id = item["user_id"]?.ToString(),
                            username = item["username"]?.ToString(),
                            name = item["name"]?.ToString(),
                            follower_count = item["follower_count"]?.ToObject<int>() ?? 0,
                            following_count = item["following_count"]?.ToObject<int>() ?? 0,
                            description = item["description"]?.ToString(),
                            location = item["location"]?.ToString(),
                            profile_pic_url = item["profile_pic_url"]?.ToString()
                        });
                    }
                }
                else
                {
                    // Try the deeply nested structure (for other APIs)
                    var entries = jObj["result"]?["timeline"]?["instructions"]?[0]?["entries"];
                    if (entries != null)
                    {
                        foreach (var entry in entries)
                        {
                            var tweetResults = entry["content"]?["itemContent"]?["tweet_results"]?["result"];
                            var tweet = tweetResults?["tweet"] ?? tweetResults;
                            var userLegacy = tweet?["core"]?["user_results"]?["result"]?["legacy"];

                            if (userLegacy != null)
                            {
                                apiUsers.Add(new TwitterUser
                                {
                                    user_id = userLegacy["id_str"]?.ToString(),
                                    username = userLegacy["screen_name"]?.ToString(),
                                    name = userLegacy["name"]?.ToString(),
                                    follower_count = userLegacy["followers_count"]?.ToObject<int>() ?? 0,
                                    following_count = userLegacy["friends_count"]?.ToObject<int>() ?? 0,
                                    description = userLegacy["description"]?.ToString(),
                                    location = userLegacy["location"]?.ToString(),
                                    profile_pic_url = userLegacy["profile_image_url_https"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.JobInfo += $" Error parsing API response: {ex.Message}";
            }
            return apiUsers;
        }

        public IActionResult Index()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "twitterusers.json");
            ViewBag.JsonImported = System.IO.File.Exists(path) && new System.IO.FileInfo(path).Length > 0;
            return View();
        }
    }
}
