using Newtonsoft.Json;

namespace Twitter_Interoperability_project.Models
{
    public class TwitterUser
    {
        [JsonProperty("id_str")]
        public string user_id { get; set; }
        [JsonProperty("screen_name")]
        public string username { get; set; }
        [JsonProperty("name")]
        public string name { get; set; }
        [JsonProperty("followers_count")]
        public int follower_count { get; set; }
        [JsonProperty("friends_count")]
        public int following_count { get; set; }
        [JsonProperty("description")]
        public string description { get; set; }
        [JsonProperty("location")]
        public string location { get; set; }
        [JsonProperty("profile_image_url_https")]
        public string profile_pic_url { get; set; }
    }
}
