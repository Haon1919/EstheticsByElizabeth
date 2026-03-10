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
        // Simple in-memory rate limiting (resets on restart)
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int Attempts, DateTimeOffset LastAttempt)> _failedLogins = new();
        private const int MaxAttempts = 5;
        private const int LockoutMinutes = 15;

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

            // Rate Limiting Check
            var ipAddress = req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            if (_failedLogins.TryGetValue(ipAddress, out var loginState))
            {
                if (loginState.Attempts >= MaxAttempts)
                {
                    if (DateTimeOffset.UtcNow < loginState.LastAttempt.AddMinutes(LockoutMinutes))
                    {
                        _logger.LogWarning("🚫 Rate limit exceeded for IP: {IpAddress}", ipAddress);
                        return new ObjectResult(new { success = false, message = "Too many failed attempts. Please try again later." })
                        {
                            StatusCode = StatusCodes.Status429TooManyRequests
                        };
                    }
                    else
                    {
                        // Lockout expired, reset
                         _failedLogins.TryRemove(ipAddress, out _);
                    }
                }
            }

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
                    // Successful login - clear failures
                    _failedLogins.TryRemove(ipAddress, out _);

                    var token = AuthTokenService.GenerateToken();
                    return new OkObjectResult(new
                    {
                        success = true,
                        data = new { token }
                    });
                }

                // Failed login - increment counter
                _failedLogins.AddOrUpdate(ipAddress, 
                    (1, DateTimeOffset.UtcNow), 
                    (key, oldState) => (oldState.Attempts + 1, DateTimeOffset.UtcNow));
                
                _logger.LogWarning("🚫 Invalid admin login attempt from IP: {IpAddress}. Attempt {AttemptCount}/{MaxAttempts}", 
                    ipAddress, _failedLogins[ipAddress].Attempts, MaxAttempts);

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
