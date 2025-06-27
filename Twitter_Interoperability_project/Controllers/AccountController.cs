using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Org.BouncyCastle.Crypto.Generators;
using Twitter_Interoperability_project.Models;

namespace Twitter_Interoperability_project.Controllers
{
    public class AccountController : Controller
    {
        private readonly Supabase.Client _supabase;
        private readonly string _secretKey;

        public AccountController(Supabase.Client supabase, IConfiguration config)
        {
            _supabase = supabase;
            _secretKey = config["Jwt:SecretKey"];
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password)
        {
            
            var userResult = await _supabase.From<User>()
                .Filter("username", Supabase.Postgrest.Constants.Operator.Equals, username)
                .Get();

            var user = userResult.Models.FirstOrDefault();
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View();
            }

            
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTime.UtcNow.AddMinutes(15) }
            );

           
            var accessToken = GenerateJwtToken(username);
            var refreshToken = Guid.NewGuid().ToString();

            await _supabase.From<RefreshToken>().Insert(new RefreshToken
            {
                UserId = user.Id,
                AccessToken = accessToken,
                refreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

           
            Response.Cookies.Append("access_token", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, 
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });
            Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return RedirectToAction("Index", "Home");
        }
        [AllowAnonymous]
        public IActionResult Register() => View();

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(string username, string password)
        {
            
            var existing = await _supabase.From<User>()
                .Filter("username", Supabase.Postgrest.Constants.Operator.Equals, username)
                .Get();
            if (existing.Models.Any())
            {
                ModelState.AddModelError("", "Username already exists.");
                return View();
            }

           
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User { Username = username, PasswordHash = hash };
            await _supabase.From<User>().Insert(user);

            TempData["Message"] = "Registration successful. Please log in.";
            return RedirectToAction("Login");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private string GenerateJwtToken(string username)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddMinutes(15),
                claims: claims,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
