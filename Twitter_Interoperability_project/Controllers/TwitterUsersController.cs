using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Twitter_Interoperability_project.Models;
using Twitter_Interoperability_project.Service;

namespace Twitter_Interoperability_project.Controllers
{
    [ApiController]
    [Route("api/twitterusers")]
    public class TwitterUsersController : Controller
    {

        // GET: api/twitterusers
        [HttpGet]
        public ActionResult<IEnumerable<TwitterUser>> GetAll()
        {
            var users = TwitterUserData.LoadUsers();
            return Ok(users);
        }

        // GET: api/twitterusers/{id}
        [HttpGet("{id}")]
        public ActionResult<TwitterUser> GetById(string id)
        {
            var users = TwitterUserData.LoadUsers();
            var user = users.FirstOrDefault(u => u.user_id == id);
            return user == null ? NotFound() : Ok(user);
        }

        // POST: api/twitterusers
        [HttpPost]
        public ActionResult<TwitterUser> Create([FromBody] TwitterUser newUser)
        {
            var users = TwitterUserData.LoadUsers();
            if (users.Any(u => u.user_id == newUser.user_id))
                return Conflict("User ID already exists");
            users.Add(newUser);
            TwitterUserData.SaveUsers(users);
            return CreatedAtAction(nameof(GetById), new { id = newUser.user_id }, newUser);
        }

        // PUT: api/twitterusers/{id}
        [HttpPut("{id}")]
        public IActionResult Update(string id, [FromBody] TwitterUser updatedUser)
        {
            var users = TwitterUserData.LoadUsers();
            var user = users.FirstOrDefault(u => u.user_id == id);
            if (user == null) return NotFound();

            user.username = updatedUser.username;
            user.name = updatedUser.name;
            user.follower_count = updatedUser.follower_count;
            user.following_count = updatedUser.following_count;
            user.description = updatedUser.description;
            user.location = updatedUser.location;
            user.profile_pic_url = updatedUser.profile_pic_url;

            TwitterUserData.SaveUsers(users);
            return NoContent();
        }

        // DELETE: api/twitterusers/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            var users = TwitterUserData.LoadUsers();
            var user = users.FirstOrDefault(u => u.user_id == id);
            if (user == null) return NotFound();
            users.Remove(user);
            TwitterUserData.SaveUsers(users);
            return NoContent();
        }

        // POST: api/twitterusers/import
        
        [HttpPost("import")]
public IActionResult ImportFromApi([FromBody] List<TwitterUser> apiUsers)
{
    var users = TwitterUserData.LoadUsers();
    int added = 0;
    foreach (var apiUser in apiUsers)
    {
        if (!users.Any(u => u.user_id == apiUser.user_id))
        {
            users.Add(apiUser);
            added++;
        }
    }
    TwitterUserData.SaveUsers(users);
    return Ok(new { message = $"Imported {added} users. Total: {users.Count}" });
}

    }
}
