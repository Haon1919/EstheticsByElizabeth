using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API
{
    public class GetAppointmentsByDate
    {
        private readonly ILogger<GetAppointmentsByDate> _logger;

        public GetAppointmentsByDate(ILogger<GetAppointmentsByDate> logger)
        {
            _logger = logger;
        }

        [Function("GetAppointmentsByDate")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
