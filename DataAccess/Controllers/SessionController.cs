using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{

    [ApiController]
    [Route("api/session")]
    public class SessionController : ControllerBase
    {
        private readonly string _playwrightDir;
        private readonly string _userDataDir;

        //introduced playwrightDir and userDataDir to manage paths for Playwright scripts and user data
        //test branch remote after feedback
        public SessionController()
        {
            // Resolve playwright directory relative to your project
            var baseDir = Directory.GetCurrentDirectory();
            _playwrightDir = Path.GetFullPath(Path.Combine(baseDir, "..", "playwright"));
            _userDataDir = Path.GetFullPath(Path.Combine(baseDir, "browser-data"));
        }

        private (string FileName, string Arguments) GetPlaywrightCommand(string mode, string? txId = null)
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            // Windows: npx.cmd, Linux/WSL: /usr/bin/npx
            //var npxPath = isWindows ? "npx.cmd" : "/usr/bin/npx";
            var npxPath = isWindows
                        ? @"C:\Program Files\nodejs\npx.cmd"  // <-- your actual path
    :                   "/usr/bin/npx";

            var args = $"playwright test gpay-scraper.js --project=chromium --headed --reporter=line";

            return (npxPath, args);
        }

        [HttpGet("status")]
        public IActionResult Status()
        {
            var sessionExists = Directory.Exists(Path.Combine(_userDataDir, "Default"));

            return Ok(new
            {
                is_logged_in = sessionExists,
                is_active = sessionExists
            });
        }

        [HttpPost("login")]
        public IActionResult Login()
        {
            Directory.CreateDirectory(_userDataDir);

            var (fileName, arguments) = GetPlaywrightCommand("login");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = _playwrightDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            // Set environment variables
            process.StartInfo.Environment["GPAY_USER_DATA_DIR"] = _userDataDir;
            process.StartInfo.Environment["GPAY_URL"] = "https://pay.google.com/business";
            process.StartInfo.Environment["GPAY_MODE"] = "login";

            try
            {
                process.Start();

                // Optional: read output for debugging
                // string output = process.StandardOutput.ReadToEnd();
                // string error = process.StandardError.ReadToEnd();

                return Ok(new
                {
                    message = "Browser opened for manual login. Please complete login in the browser window.",
                    session_id = Guid.NewGuid().ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to launch browser",
                    details = ex.Message
                });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            if (Directory.Exists(_userDataDir))
            {
                Directory.Delete(_userDataDir, true);
            }

            return Ok(new { message = "Session cleared" });
        }
    }
}