using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API
{
    public class CancelAppointment
    {
        private readonly ILogger<CancelAppointment> _logger;

        public CancelAppointment(ILogger<CancelAppointment> logger)
        {
            _logger = logger;
        }

        [Function("CancelAppointment")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
