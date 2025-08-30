using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Functions
{
    public class TableListFunction
    {
        private readonly ProjectContext _context;
        private readonly ILogger _logger;

        public TableListFunction(ProjectContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<TableListFunction>();
        }        [Function("ListTables")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "debug/tables")] HttpRequest req)
        {
            _logger.LogInformation("📋 Listing database tables structure");
            
            try
            {
                var tables = _context.Model.GetEntityTypes()
                    .Select(t => new 
                    {
                        Name = t.GetTableName(),
                        Schema = t.GetSchema(),
                        Properties = t.GetProperties()
                            .Select(p => new 
                            {
                                Name = p.Name,
                                Type = p.ClrType.Name,
                                IsKey = p.IsKey()
                            }).ToList()
                    })
                    .ToList();
                    
                _logger.LogInformation("✅ Retrieved structure for {Count} tables", tables.Count);
                return new OkObjectResult(tables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error retrieving table structure");
                return new ObjectResult("An error occurred while retrieving table structure")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
