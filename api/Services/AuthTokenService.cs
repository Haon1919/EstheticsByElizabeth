using System;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace API.Services
{
    public interface IAuthTokenService
    {
        string GenerateToken();
        bool ValidateRequest(HttpRequest req);
    }

    /// <summary>
    /// Issues and validates JWT tokens for admin authentication.
    /// Tokens are stateless so they work across multiple function instances.
    /// </summary>
    public class AuthTokenService : IAuthTokenService
    {
        private readonly string? _secret;

        public AuthTokenService(IConfiguration configuration)
        {
            _secret = configuration["ADMIN_JWT_SECRET"] ?? Environment.GetEnvironmentVariable("ADMIN_JWT_SECRET");
        }

        private byte[] GetValidKey()
        {
            if (string.IsNullOrEmpty(_secret))
            {
                throw new InvalidOperationException("ADMIN_JWT_SECRET is not configured");
            }

            var keyBytes = Encoding.UTF8.GetBytes(_secret);
            
            // HMAC SHA256 requires at least 256 bits (32 bytes)
            if (keyBytes.Length < 32)
            {
                throw new InvalidOperationException($"ADMIN_JWT_SECRET must be at least 32 characters long. Current length: {keyBytes.Length}");
            }

            return keyBytes;
        }

        public string GenerateToken()
        {
            var handler = new JwtSecurityTokenHandler();
            var key = GetValidKey();
            var descriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddHours(12),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };
            var token = handler.CreateToken(descriptor);
            return handler.WriteToken(token);
        }

        public bool ValidateRequest(HttpRequest req)
        {
            try
            {
                var key = GetValidKey();

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
                        IssuerSigningKey = new SymmetricSecurityKey(key)
                    };
                    handler.ValidateToken(token, validations, out _);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}