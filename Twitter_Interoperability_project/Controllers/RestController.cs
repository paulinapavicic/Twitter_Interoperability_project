using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using Twitter_Interoperability_project.Models;


namespace Twitter_Interoperability_project.Controllers
{
    [Authorize]
    public class RestController : Controller
    {

        private readonly string apiUrl = "https://twitter154.p.rapidapi.com/user/tweets";
        private readonly string apiKey = "f0c3be805emsh767cbcd3c6c4040p18cb0ejsndcda90a0978b";
        private readonly string apiHost = "twitter154.p.rapidapi.com";
      
        private const string dataPath = "App_Data/twitterusers.json";


        [HttpPost]
        public async Task<IActionResult> ImportJson(string username)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-RapidAPI-Key", apiKey);
                    client.DefaultRequestHeaders.Add("X-RapidAPI-Host", apiHost);

                    var url = $"{apiUrl}?username={Uri.EscapeDataString(username)}";
                    var response = await client.GetAsync(url);
                    var json = await response.Content.ReadAsStringAsync();

                   
                    var apiResponse = JObject.Parse(json);
                    var users = apiResponse["results"]? 
                        .Select(t => t["user"]?.ToObject<TwitterUser>())
                        .Where(u => u != null)
                        .ToList();

                    if (users == null || users.Count == 0)
                    {
                        ViewBag.JobInfo = "No user data found in the response.";
                        return View("Index");
                    }

                   
                    SaveUsers(users);
                    ViewBag.JsonImported = true;
                    ViewBag.JobInfo = $"Imported {users.Count} users successfully!";
                }
            }
            catch (Exception ex)
            {
                ViewBag.JsonImported = false;
                ViewBag.JobInfo = $"Error: {ex.Message}";
            }
            return View("Index");
        }




        [HttpPost]
        public IActionResult CreateUser(TwitterUser user)
        {
            var users = LoadUsers();
            int nextId = users.Any()
                ? users.Select(u => int.TryParse(u.user_id, out var id) ? id : 0).Max() + 1
                : 1;
            user.user_id = nextId.ToString();

            users.Add(user);
            SaveUsers(users);
            TempData["Message"] = $"User created successfully with ID {user.user_id}!";
            return RedirectToAction("Index");
        }


        [HttpPost]
        public IActionResult UpdateUser(string userId, TwitterUser updatedUser)
        {
            var users = LoadUsers();
            var user = users.FirstOrDefault(u => u.user_id == userId);
            if (user == null)
            {
                TempData["Message"] = "User not found!";
                return RedirectToAction("Index");
            }

         
            user.username = updatedUser.username;
            user.name = updatedUser.name;
            user.follower_count = updatedUser.follower_count;
            user.following_count = updatedUser.following_count;
            user.description = updatedUser.description;
            user.location = updatedUser.location;
            user.profile_pic_url = updatedUser.profile_pic_url;

            SaveUsers(users);
            TempData["Message"] = "User updated successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteUser(string userId)
        {
            var users = LoadUsers();
            var user = users.FirstOrDefault(u => u.user_id == userId);
            if (user == null)
            {
                TempData["Message"] = "User not found!";
                return RedirectToAction("Index");
            }
            users.Remove(user);
            SaveUsers(users);
            TempData["Message"] = "User deleted successfully!";
            return RedirectToAction("Index");
        }

       
        private List<TwitterUser> LoadUsers()
        {
            if (!System.IO.File.Exists(dataPath))
            {
                
                Directory.CreateDirectory(Path.GetDirectoryName(dataPath)!);
                System.IO.File.WriteAllText(dataPath, "[]");
                return new List<TwitterUser>();
            }

            var json = System.IO.File.ReadAllText(dataPath);
            try
            {
                return JsonConvert.DeserializeObject<List<TwitterUser>>(json) ?? new List<TwitterUser>();
            }
            catch
            {
                
                System.IO.File.WriteAllText(dataPath, "[]");
                return new List<TwitterUser>();
            }
        }


        private void SaveUsers(List<TwitterUser> users)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dataPath)!);
            System.IO.File.WriteAllText(dataPath, JsonConvert.SerializeObject(users, Formatting.Indented));
        }


        public IActionResult Index()
        {

            ViewBag.Users = LoadUsers(); 
            ViewBag.Imported = System.IO.File.Exists(dataPath) && new System.IO.FileInfo(dataPath).Length > 0;
            return View();
        }

    }
}


