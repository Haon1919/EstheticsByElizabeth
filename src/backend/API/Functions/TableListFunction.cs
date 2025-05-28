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
        }

        [Function("ListTables")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "debug/tables")] HttpRequest req)
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
                
            return new OkObjectResult(tables);
        }
    }
}
