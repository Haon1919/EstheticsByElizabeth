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
    public class LlmFeatures
    {
        private readonly IAuthTokenService _authTokenService;
        private readonly ILogger<LlmFeatures> _logger;
        private readonly ILlmService _llmService;

        public LlmFeatures(ILogger<LlmFeatures> logger, ILlmService llmService, IAuthTokenService authTokenService)
        {
            _authTokenService = authTokenService;
            _logger = logger;
            _llmService = llmService;
        }

        [Function("GenerateServiceContent")]
        public async Task<IActionResult> GenerateServiceContent(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/llm/generate-service")] HttpRequest req)
        {
            if (!_authTokenService.ValidateRequest(req)) return new UnauthorizedResult();

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<GenerateServiceRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data == null || string.IsNullOrEmpty(data.Name)) return new BadRequestResult();

            var result = await _llmService.GenerateServiceContentAsync(data.Name, data.Price);
            
            return new OkObjectResult(new
            {
                description = result.Description,
                aftercare = result.Aftercare
            });
        }

        [Function("SummarizeClient")]
        public async Task<IActionResult> SummarizeClient(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/llm/summarize-client/{id}")] HttpRequest req,
            int id)
        {
            if (!_authTokenService.ValidateRequest(req)) return new UnauthorizedResult();

            var summary = await _llmService.SummarizeClientHistoryAsync(id);
            return new OkObjectResult(new { summary });
        }
    }

    public class GenerateServiceRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
