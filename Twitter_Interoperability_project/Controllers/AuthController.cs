using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Twitter_Interoperability_project.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : Controller
    {
        // In-memory for demo; use a database for production
        private static Dictionary<string, string> refreshTokens = new Dictionary<string, string>();

        private readonly string secretKey;

        public AuthController(IConfiguration configuration)
        {
            secretKey = configuration["Jwt:SecretKey"];
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model) // Make method async
        {
            // Demo: Accept any username/password
            var claims = new[]
            {
        new Claim(ClaimTypes.Name, model.Username)
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var accessToken = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddMinutes(5),
                claims: claims,
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);

            var refreshToken = Guid.NewGuid().ToString();
            refreshTokens[refreshToken] = model.Username;

            
            return Ok(new
            {
                access_token = tokenString,
                refresh_token = refreshToken
            });
        }

        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] RefreshRequest request)
        {
            if (!refreshTokens.ContainsKey(request.RefreshToken))
                return Unauthorized();

            var username = refreshTokens[request.RefreshToken];
            var claims = new[]
            {
            new Claim(ClaimTypes.Name, username)
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var accessToken = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddMinutes(5),
                claims: claims,
                signingCredentials: creds);

            // Optionally: generate a new refresh token here

            return Ok(new
            {
                access_token = new JwtSecurityTokenHandler().WriteToken(accessToken)
            });
        }
    }
}


public class LoginModel
{
    public string Username { get; set; }
    public string Password { get; set; }
}
public class RefreshRequest
{
    public string RefreshToken { get; set; }
}