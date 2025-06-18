using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Twitter_Interoperability_project.Models
{
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("username")]
        public string Username { get; set; }

        [Column("password_hash")]
        public string PasswordHash { get; set; }
    }
}
