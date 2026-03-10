using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Entities;
using API.Services;
using System.Collections.Generic;
using System.Linq;

namespace API.Functions
{
    public class ManageSettings
    {
        private readonly ILogger<ManageSettings> _logger;
        private readonly ProjectContext _context;

        public ManageSettings(ILogger<ManageSettings> logger, ProjectContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Function("GetSettings")]
        public async Task<IActionResult> GetSettings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/settings")] HttpRequest req)
        {
            if (!AuthTokenService.ValidateRequest(req)) return new UnauthorizedResult();

            var settings = await _context.SystemSettings.ToListAsync();
            var result = settings.ToDictionary(s => s.Key, s => s.Value);
            
            // Mask API key for security when returning to UI
            if (result.ContainsKey("LlmApiKey") && !string.IsNullOrEmpty(result["LlmApiKey"]))
            {
                result["LlmApiKey"] = "********";
            }

            return new OkObjectResult(result);
        }

        [Function("UpdateSettings")]
        public async Task<IActionResult> UpdateSettings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "manage/settings")] HttpRequest req)
        {
            if (!AuthTokenService.ValidateRequest(req)) return new UnauthorizedResult();

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updates = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);

            if (updates == null) return new BadRequestResult();

            foreach (var kvp in updates)
            {
                // Don't overwrite API key if it's just the masked version
                if (kvp.Key == "LlmApiKey" && kvp.Value == "********") continue;

                var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == kvp.Key);
                if (setting == null)
                {
                    _context.SystemSettings.Add(new SystemSetting { Key = kvp.Key, Value = kvp.Value });
                }
                else
                {
                    setting.Value = kvp.Value;
                }
            }

            await _context.SaveChangesAsync();
            return new OkObjectResult(new { success = true });
        }
    }
}
