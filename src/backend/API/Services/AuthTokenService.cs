using System;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace API.Services
{
    /// <summary>
    /// Issues and validates JWT tokens for admin authentication.
    /// Tokens are stateless so they work across multiple function instances.
    /// </summary>
    public static class AuthTokenService
    {
        private static readonly string? _secret = Environment.GetEnvironmentVariable("ADMIN_JWT_SECRET");

        public static string GenerateToken()
        {
            if (string.IsNullOrEmpty(_secret))
            {
                throw new InvalidOperationException("ADMIN_JWT_SECRET is not configured");
            }

            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secret);
            var descriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddHours(12),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };
            var token = handler.CreateToken(descriptor);
            return handler.WriteToken(token);
        }

        public static bool ValidateRequest(HttpRequest req)
        {
            if (string.IsNullOrEmpty(_secret))
            {
                return false;
            }

            if (!req.Headers.TryGetValue("Authorization", out var authHeader))
            {
                return false;
            }

            var header = authHeader.ToString();
            if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var token = header.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();

            try
            {
                var validations = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret))
                };
                handler.ValidateToken(token, validations, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
