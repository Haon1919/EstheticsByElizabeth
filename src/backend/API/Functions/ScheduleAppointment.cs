using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API
{
    public class ScheduleAppointment
    {
        private readonly ILogger<ScheduleAppointment> _logger;

        public ScheduleAppointment(ILogger<ScheduleAppointment> logger)
        {
            _logger = logger;
        }

        [Function("ScheduleAppointment")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
