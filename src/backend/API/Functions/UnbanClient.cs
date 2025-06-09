using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using Microsoft.EntityFrameworkCore;
using API.Entities;
using API.Services;

namespace API.Functions
{
    /// <summary>
    /// ✅ Client Unbanning Function
    /// Handles removing ban status from clients
    /// </summary>
    public class UnbanClient
    {
        private readonly ILogger<UnbanClient> _logger;
        private readonly ProjectContext _context;
        private readonly ClientReviewService _reviewService;

        public UnbanClient(
            ILogger<UnbanClient> logger, 
            ProjectContext context,
            ClientReviewService reviewService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
        }

        /// <summary>
        /// ✅ Removes a ban from a client
        /// </summary>
        [Function("UnbanClient")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "clients/{clientId}/ban")] HttpRequest req,
            string clientId)
        {
            _logger.LogInformation("✅ Processing unban request for client ID: {ClientId}", clientId);

            try
            {
                if (!int.TryParse(clientId, out int id))
                {
                    _logger.LogWarning("🚫 Invalid client ID format: {ClientId}", clientId);
                    return new BadRequestObjectResult("Invalid client ID format");
                }

                // Find the client
                _logger.LogInformation("🔍 Looking for client with ID: {ClientId}", id);
                var client = await _context.Clients
                    .Include(c => c.ReviewFlags)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (client == null)
                {
                    _logger.LogWarning("❌ Client not found with ID: {ClientId}", clientId);
                    return new NotFoundObjectResult($"Client with ID {clientId} not found");
                }

                // Find any active ban flags
                var banFlags = client.ReviewFlags
                    .Where(rf => rf.Status.ToUpper() == "BANNED")
                    .ToList();

                if (banFlags.Count == 0)
                {
                    _logger.LogWarning("⚠️ Client with ID {ClientId} is not currently banned", clientId);
                    return new BadRequestObjectResult($"Client with ID {clientId} is not currently banned");
                }

                // Update all ban flags to "RESOLVED"
                _logger.LogInformation("🛠️ Resolving {Count} ban flags for client {ClientId}", banFlags.Count, clientId);
                foreach (var flag in banFlags)
                {
                    flag.Status = "RESOLVED";
                    flag.ReviewDate = DateTimeOffset.UtcNow;
                    flag.ReviewedBy = "System (Unban Request)";
                    flag.AdminComments = "Ban removed via DELETE endpoint";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Client with ID {ClientId} has been successfully unbanned", clientId);

                return new OkObjectResult(new { 
                    Message = $"Client with ID {clientId} has been unbanned and can now make appointments",
                    UnbannedFlags = banFlags.Count
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "💥 Database update failed while unbanning client {ClientId}. Inner: {InnerMessage}", 
                    clientId, dbEx.InnerException?.Message);
                return new ObjectResult($"🔥 A database error occurred: {dbEx.InnerException?.Message ?? dbEx.Message}")
                {
                    StatusCode = StatusCodes.Status409Conflict
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error processing unban request for client ID: {ClientId}", clientId);
                return new ObjectResult($"An unexpected error occurred while processing unban request for client ID {clientId}")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
