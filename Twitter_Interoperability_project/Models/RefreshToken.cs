using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Twitter_Interoperability_project.Models
{

    [Table("tokens")]

    public class RefreshToken : BaseModel
    {
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("access_token")]
        public string AccessToken { get; set; }

        [Column("refresh_token")]
        public string refreshToken { get; set; }

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }
    }
}
