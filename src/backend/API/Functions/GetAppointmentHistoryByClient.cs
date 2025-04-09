using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API
{
    public class GetAppointmentHistoryByClient
    {
        private readonly ILogger<GetAppointmentHistoryByClient> _logger;

        public GetAppointmentHistoryByClient(ILogger<GetAppointmentHistoryByClient> logger)
        {
            _logger = logger;
        }

        [Function("GetAppointmentHistoryByClient")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
