using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API
{
    public class GetAppointment
    {
        private readonly ILogger<GetAppointment> _logger;

        public GetAppointment(ILogger<GetAppointment> logger)
        {
            _logger = logger;
        }

        [Function("GetAppointment")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
