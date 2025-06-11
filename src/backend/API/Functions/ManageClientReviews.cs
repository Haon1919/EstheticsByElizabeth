using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using API.Data;
using API.Entities;
using API.Services;

namespace API.Functions
{
    /// <summary>
    /// 🚩 Client Review Management 🚩
    /// Functions for managing client review flags.
    /// </summary>
    public class ManageClientReviews
    {
        private readonly ILogger<ManageClientReviews> _logger;
        private readonly ProjectContext _context;
        private readonly ClientReviewService _reviewService;

        public ManageClientReviews(
            ILogger<ManageClientReviews> logger, 
            ProjectContext context,
            ClientReviewService reviewService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
        }

        /// <summary>
        /// 📋 Get all client review flags
        /// </summary>
        [Function("GetClientReviewFlags")]
        public async Task<IActionResult> GetFlags(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "client-reviews")] HttpRequest req)
        {
            _logger.LogInformation("📋 Fetching client review flags");

            try
            {
                string? status = req.Query["status"];
                
                var query = _context.ClientReviewFlags
                    .Include(rf => rf.Client)
                    .AsQueryable();

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(rf => rf.Status == status);
                }

                // Get the flags
                var flags = await query
                    .OrderByDescending(rf => rf.FlagDate)
                    .Select(rf => new
                    {
                        rf.Id,
                        rf.ClientId,
                        rf.AppointmentId,
                        rf.FlagReason,
                        rf.FlagDate,
                        rf.Status,
                        rf.ReviewDate,
                        rf.ReviewedBy,
                        rf.AdminComments,
                        rf.AutoFlags,
                        Client = new
                        {
                            rf.Client.Id,
                            rf.Client.FirstName,
                            rf.Client.LastName,
                            rf.Client.Email,
                            rf.Client.PhoneNumber
                        },
                        Appointment = new
                        {
                            rf.Appointment.Id,
                            rf.Appointment.Time,
                            ServiceId = rf.Appointment.ServiceId
                        }
                    })
                    .ToListAsync();

                return new OkObjectResult(flags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client review flags");
                return new ObjectResult("An error occurred while fetching client review flags")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// 🔍 Get a specific client review flag
        /// </summary>
        [Function("GetClientReviewFlag")]
        public async Task<IActionResult> GetFlag(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "client-reviews/{id}")] HttpRequest req,
            string id)
        {
            _logger.LogInformation("🔍 Fetching client review flag with ID: {FlagId}", id);

            try
            {
                if (!int.TryParse(id, out int flagId))
                {
                    return new BadRequestObjectResult("Invalid flag ID format");
                }

                var flag = await _context.ClientReviewFlags
                    .Include(rf => rf.Client)
                    .Include(rf => rf.Appointment)
                    .Where(rf => rf.Id == flagId)
                    .Select(rf => new
                    {
                        rf.Id,
                        rf.ClientId,
                        rf.AppointmentId,
                        rf.FlagReason,
                        rf.FlagDate,
                        rf.Status,
                        rf.ReviewDate,
                        rf.ReviewedBy,
                        rf.AdminComments,
                        rf.AutoFlags,
                        Client = new
                        {
                            rf.Client.Id,
                            rf.Client.FirstName,
                            rf.Client.LastName,
                            rf.Client.Email,
                            rf.Client.PhoneNumber
                        },
                        Appointment = new
                        {
                            rf.Appointment.Id,
                            rf.Appointment.Time,
                            ServiceId = rf.Appointment.ServiceId
                        }
                    })
                    .FirstOrDefaultAsync();

                if (flag == null)
                {
                    return new NotFoundObjectResult($"Review flag with ID {id} not found");
                }

                return new OkObjectResult(flag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client review flag with ID: {FlagId}", id);
                return new ObjectResult($"An error occurred while fetching client review flag with ID {id}")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// ✏️ Update a client review flag
        /// </summary>
        [Function("UpdateClientReviewFlag")]
        public async Task<IActionResult> UpdateFlag(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "client-reviews/{id}")] HttpRequest req,
            string id)
        {
            _logger.LogInformation("✏️ Updating client review flag with ID: {FlagId}", id);

            try
            {
                if (!int.TryParse(id, out int flagId))
                {
                    return new BadRequestObjectResult("Invalid flag ID format");
                }

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var updateData = JsonSerializer.Deserialize<UpdateReviewFlagDto>(
                    requestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (updateData == null || string.IsNullOrEmpty(updateData.Status))
                {
                    return new BadRequestObjectResult("Invalid or incomplete update data");
                }

                var result = await _reviewService.UpdateClientReviewFlagStatusAsync(
                    flagId,
                    updateData.Status,
                    updateData.ReviewedBy ?? "Admin",
                    updateData.AdminComments ?? "");

                if (!result)
                {
                    return new NotFoundObjectResult($"Review flag with ID {id} not found or could not be updated");
                }

                return new OkObjectResult(new { Message = $"Review flag with ID {id} successfully updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client review flag with ID: {FlagId}", id);
                return new ObjectResult($"An error occurred while updating client review flag with ID {id}")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// 👤 Get all pending reviews for a specific client
        /// </summary>
        [Function("GetClientPendingReviews")]
        public async Task<IActionResult> GetClientPendingReviews(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "clients/{clientId}/reviews")] HttpRequest req,
            string clientId)
        {
            _logger.LogInformation("👤 Fetching pending reviews for client ID: {ClientId}", clientId);

            try
            {
                if (!int.TryParse(clientId, out int id))
                {
                    return new BadRequestObjectResult("Invalid client ID format");
                }

                var flags = await _context.ClientReviewFlags
                    .Include(rf => rf.Appointment)
                    .Where(rf => rf.ClientId == id)
                    .OrderByDescending(rf => rf.FlagDate)
                    .Select(rf => new 
                    {
                        rf.Id,
                        rf.ClientId,
                        rf.AppointmentId,
                        rf.FlagReason,
                        rf.FlagDate,
                        rf.Status,
                        rf.ReviewDate,
                        rf.ReviewedBy,
                        rf.AdminComments,
                        rf.AutoFlags,
                        Appointment = new
                        {
                            rf.Appointment.Id,
                            rf.Appointment.Time,
                            ServiceId = rf.Appointment.ServiceId
                        }
                    })
                    .ToListAsync();

                return new OkObjectResult(flags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending reviews for client ID: {ClientId}", clientId);
                return new ObjectResult($"An error occurred while fetching pending reviews for client ID {clientId}")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// 🚫 Ban or unban a client from making appointments
        /// </summary>
        [Function("BanClient")]
        public async Task<IActionResult> BanClient(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "clients/{clientId}/ban")] HttpRequest req,
            string clientId)
        {
            _logger.LogInformation("🚫 Processing ban request for client ID: {ClientId}", clientId);

            try
            {
                if (!int.TryParse(clientId, out int id))
                {
                    return new BadRequestObjectResult("Invalid client ID format");
                }

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var banData = JsonSerializer.Deserialize<BanClientDto>(
                    requestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (banData == null)
                {
                    return new BadRequestObjectResult("Invalid or incomplete ban data");
                }

                var result = await _reviewService.UpdateClientBanStatusAsync(
                    id,
                    banData.IsBanned,
                    banData.Reason ?? "Manual ban by administrator",
                    banData.AdminName ?? "Admin",
                    banData.Comments ?? "");

                if (!result)
                {
                    return new NotFoundObjectResult($"Client with ID {clientId} not found or could not be updated");
                }

                string message = banData.IsBanned
                    ? $"Client with ID {clientId} has been banned from making appointments"
                    : $"Client with ID {clientId} has been unbanned and can now make appointments";

                return new OkObjectResult(new { Message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ban request for client ID: {ClientId}", clientId);
                return new ObjectResult($"An error occurred while processing ban request for client ID {clientId}")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// ➕ Create a new client review flag
        /// </summary>
        [Function("CreateClientReviewFlag")]
        public async Task<IActionResult> CreateFlag(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "client-reviews")] HttpRequest req)
        {
            _logger.LogInformation("➕ Creating new client review flag");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var createData = JsonSerializer.Deserialize<CreateReviewFlagDto>(
                    requestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (createData == null || createData.ClientId <= 0 || createData.AppointmentId <= 0)
                {
                    return new BadRequestObjectResult("Invalid or incomplete review flag data. ClientId and AppointmentId are required.");
                }

                // Check if client exists
                var client = await _context.Clients.FindAsync(createData.ClientId);
                if (client == null)
                {
                    return new BadRequestObjectResult($"Client with ID {createData.ClientId} not found");
                }

                // Check if appointment exists
                var appointment = await _context.Appointments.FindAsync(createData.AppointmentId);
                if (appointment == null)
                {
                    return new BadRequestObjectResult($"Appointment with ID {createData.AppointmentId} not found");
                }

                var newFlag = new ClientReviewFlag
                {
                    ClientId = createData.ClientId,
                    AppointmentId = createData.AppointmentId,
                    FlagReason = createData.FlagReason ?? "Test flag",
                    FlagDate = DateTimeOffset.UtcNow,
                    Status = createData.Status ?? "PENDING",
                    AutoFlags = createData.AutoFlags ?? 1
                };

                _context.ClientReviewFlags.Add(newFlag);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new review flag with ID: {FlagId}", newFlag.Id);

                return new CreatedResult($"/api/client-reviews/{newFlag.Id}", new
                {
                    Id = newFlag.Id,
                    ClientId = newFlag.ClientId,
                    AppointmentId = newFlag.AppointmentId,
                    FlagReason = newFlag.FlagReason,
                    FlagDate = newFlag.FlagDate,
                    Status = newFlag.Status,
                    AutoFlags = newFlag.AutoFlags
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating client review flag");
                return new ObjectResult("An error occurred while creating client review flag")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// 🗑️ Delete a client review flag
        /// </summary>
        [Function("DeleteClientReviewFlag")]
        public async Task<IActionResult> DeleteFlag(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "client-reviews/{id}")] HttpRequest req,
            string id)
        {
            _logger.LogInformation("🗑️ Deleting client review flag with ID: {FlagId}", id);

            try
            {
                if (!int.TryParse(id, out int flagId))
                {
                    return new BadRequestObjectResult("Invalid flag ID format");
                }

                var flag = await _context.ClientReviewFlags.FindAsync(flagId);
                if (flag == null)
                {
                    return new NotFoundObjectResult($"Review flag with ID {id} not found");
                }

                _context.ClientReviewFlags.Remove(flag);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted review flag with ID: {FlagId}", flagId);

                return new OkObjectResult(new { Message = $"Review flag with ID {id} successfully deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting client review flag with ID: {FlagId}", id);
                return new ObjectResult($"An error occurred while deleting client review flag with ID {id}")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }

    // DTO for creating review flags
    public class CreateReviewFlagDto
    {
        public int ClientId { get; set; }
        public int AppointmentId { get; set; }
        public string? FlagReason { get; set; }
        public string? Status { get; set; }
        public int? AutoFlags { get; set; }
    }

    // DTO for updating review flags
    public class UpdateReviewFlagDto
    {
        public string Status { get; set; } = string.Empty;
        public string? ReviewedBy { get; set; }
        public string? AdminComments { get; set; }
    }

    // DTO for banning clients
    public class BanClientDto
    {
        public bool IsBanned { get; set; } = true;
        public string? Reason { get; set; }
        public string? AdminName { get; set; }
        public string? Comments { get; set; }
    }
}
