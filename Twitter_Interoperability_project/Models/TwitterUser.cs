using Newtonsoft.Json;

namespace Twitter_Interoperability_project.Models
{
    public class TwitterUser
    {
        public string user_id { get; set; }
        public string username { get; set; }
        public string name { get; set; }
        public int follower_count { get; set; }
        public int following_count { get; set; }
        public string description { get; set; }
        public string location { get; set; }
        public string profile_pic_url { get; set; }
    }
}
