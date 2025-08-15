using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using API.Services;

namespace API.Functions
{
    public class AdminLogin
    {
        private readonly ILogger<AdminLogin> _logger;

        public AdminLogin(ILogger<AdminLogin> logger)
        {
            _logger = logger;
        }

        [Function("AdminLogin")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "auth/admin")] HttpRequest req)
        {
            // Handle CORS preflight
            if (req.Method == HttpMethods.Options)
            {
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                return new OkResult();
            }

            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var loginRequest = JsonSerializer.Deserialize<LoginRequest>(requestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Password))
                {
                    return new BadRequestObjectResult(new { success = false, message = "Password is required." });
                }

                var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

                if (loginRequest.Password == adminPassword)
                {
                    var token = AuthTokenService.GenerateToken();
                    return new OkObjectResult(new
                    {
                        success = true,
                        data = new { token }
                    });
                }

                return new UnauthorizedObjectResult(new { success = false, message = "Invalid credentials." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing admin login request");
                return new ObjectResult(new { success = false, message = ex.Message })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        private class LoginRequest
        {
            public string Password { get; set; } = string.Empty;
        }
    }
}
