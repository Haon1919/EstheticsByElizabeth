using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace API.Services
{
    /// <summary>
    /// Simple in-memory token store for admin authentication.
    /// Generates and validates session tokens issued at login.
    /// </summary>
    public static class AuthTokenService
    {
        private static readonly ConcurrentDictionary<string, DateTime> _tokens = new();

        public static string GenerateToken()
        {
            var token = Guid.NewGuid().ToString();
            _tokens[token] = DateTime.UtcNow;
            return token;
        }

        public static bool ValidateRequest(HttpRequest req)
        {
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
            return _tokens.ContainsKey(token);
        }
    }
}
