using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DataAccess.Controllers
{

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly string _jwtKey;

        public AuthController(IConfiguration config)
        {
            _jwtKey = config["Jwt:Key"] ?? "pmms360_super_secret_key_2026_very_strong_key";
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] Models.LoginRequest req)
        {
            if (req.Username == "admin" && req.Password == "admin123")
            {
                var user = new { id = "1", username = "admin", role = "admin" };

                var token = GenerateJwtToken(user.id, user.role, _jwtKey);

                return Ok(new { user, token });
            }

            return Unauthorized(new { error = "Invalid username or password" });

        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            // 1. In production, extract user identity from the Authorization Bearer token header
            // 2. Return the matching user object structure expected by the React frontend:

            return Ok(new

            {
                user = new
                {
                    id = "user-admin",
                    username = "admin",
                    fullName = "Alex Mercer",
                    role = "admin",
                    email = "admin@pmms360.com"
                }

            });
        }

    private string GenerateJwtToken(string userId, string role, string key)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("id", userId),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}