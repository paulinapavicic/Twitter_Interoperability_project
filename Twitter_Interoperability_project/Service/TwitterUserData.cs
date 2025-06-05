using Newtonsoft.Json;
using Twitter_Interoperability_project.Models;

namespace Twitter_Interoperability_project.Service
{
    public static class TwitterUserData
    {
        // Use absolute path to ensure file is saved correctly
        private static readonly string DataPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "App_Data",
            "twitterusers.json"
        );

        public static List<TwitterUser> LoadUsers()
        {
            if (!File.Exists(DataPath))
                return new List<TwitterUser>();

            var json = File.ReadAllText(DataPath);
            return JsonConvert.DeserializeObject<List<TwitterUser>>(json) ?? new List<TwitterUser>();
        }

        public static void SaveUsers(List<TwitterUser> users)
        {
            // Ensure the App_Data directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(DataPath)!);
            var json = JsonConvert.SerializeObject(users, Formatting.Indented);
            File.WriteAllText(DataPath, json);
        }

        public static void MergeApiUsers(List<TwitterUser> apiUsers)
        {
            var users = LoadUsers();
            foreach (var apiUser in apiUsers)
            {
                if (!users.Any(u => u.user_id == apiUser.user_id))
                    users.Add(apiUser);
            }
            SaveUsers(users);
        }

        public static string GenerateNewId(List<TwitterUser> users)
        {
            var maxId = users
                .Select(u => int.TryParse(u.user_id, out var id) ? id : 0)
                .DefaultIfEmpty(0)
                .Max();
            return (maxId + 1).ToString();
        }
    }
}
