using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;

namespace DataAccess.Controllers
{

    [ApiController]
    [Route("api/auth")]
    [Authorize]
    public class AuthController : ControllerBase
    {
        [Authorize]
        [HttpPost("login")]
        public IActionResult Login([FromBody] Models.LoginRequest req)
        {
            if (req.Username == "admin" && req.Password == "admin123")
            {
                var user = new { id = "1", username = "admin", role = "admin" };
                var token = "jwt-token-here";
                return Ok(new { user, token });
            }

            return Unauthorized();
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
            
    }
}