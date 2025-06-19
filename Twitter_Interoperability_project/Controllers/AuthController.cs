using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Generators;
using Supabase;
using Supabase.Gotrue;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Twitter_Interoperability_project.Models;
using static Supabase.Postgrest.Constants;
using User = Twitter_Interoperability_project.Models.User;

namespace Twitter_Interoperability_project.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly string _secretKey;
        private readonly Supabase.Client _supabase;

        public AuthController(IConfiguration config, Supabase.Client supabase)
        {
            _secretKey = config["Jwt:SecretKey"];
            _supabase = supabase;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var userResult = await _supabase.From<User>()
                .Filter("username", Supabase.Postgrest.Constants.Operator.Equals, model.Username)
                .Get();

            var user = userResult.Models.FirstOrDefault();
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                return Unauthorized();

            
            var accessToken = GenerateJwtToken(user.Username);
            var refreshToken = Guid.NewGuid().ToString();

            await _supabase.From<RefreshToken>().Insert(new RefreshToken
            {
                UserId = user.Id,
                AccessToken = accessToken,
                refreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new { access_token = accessToken, refresh_token = refreshToken });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var tokenResult = await _supabase.From<RefreshToken>()
                .Filter("refresh_token", Operator.Equals, request.RefreshToken)
                .Get();

            var token = tokenResult.Models.FirstOrDefault();
            if (token == null || token.ExpiresAt < DateTime.UtcNow)
                return Unauthorized();

            
            var userResult = await _supabase.From<User>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, token.UserId)
                .Get();
            var user = userResult.Models.FirstOrDefault();
            if (user == null)
                return Unauthorized();

            var newAccessToken = GenerateJwtToken(user.Username);
            var newRefreshToken = Guid.NewGuid().ToString();

            
            
            token.ExpiresAt = DateTime.UtcNow.AddDays(7);
            await token.Update<RefreshToken>();

            return Ok(new { access_token = newAccessToken, refresh_token = newRefreshToken });
        }

        private string GenerateJwtToken(string username)
        {
            var claims = new[] { new Claim(ClaimTypes.Name, username) };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddMinutes(15),
                claims: claims,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
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
}





 



