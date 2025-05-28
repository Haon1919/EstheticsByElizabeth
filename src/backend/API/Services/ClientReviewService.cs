using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using API.Data;
using API.Entities;

namespace API.Services
{
    /// <summary>
    /// 🔍 Service for handling client review status checks and flags.
    /// </summary>
    public class ClientReviewService
    {
        private readonly ILogger<ClientReviewService> _logger;
        private readonly ProjectContext _context;

        public ClientReviewService(ILogger<ClientReviewService> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Checks if a client is under review.
        /// </summary>
        public async Task<bool> IsClientUnderReviewAsync(string clientEmail)
        {
            if (string.IsNullOrEmpty(clientEmail))
            {
                return false;
            }

            try
            {
                return await _context.Clients
                    .Where(c => c.Email == clientEmail)
                    .SelectMany(c => c.ReviewFlags)
                    .AnyAsync(rf => rf.Status == "Rejected" || rf.Status == "Banned");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if client {ClientEmail} is under review", clientEmail);
                return false; // Default to allowing bookings in case of errors
            }
        }

        /// <summary>
        /// Checks if a client has any existing appointments on a specific date.
        /// </summary>
        public async Task<int> GetClientAppointmentCountForDateAsync(string clientEmail, DateTimeOffset date)
        {
            if (string.IsNullOrEmpty(clientEmail))
            {
                return 0;
            }

            try
            {
                var startDate = date.Date;
                var endDate = startDate.AddDays(1);

                return await _context.Appointments
                    .CountAsync(a => a.Client.Email == clientEmail && 
                                    a.Time >= startDate && 
                                    a.Time < endDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking appointment count for client {ClientEmail} on {Date}", 
                    clientEmail, date.ToString("yyyy-MM-dd"));
                return 0;
            }
        }

        /// <summary>
        /// Creates a review flag for a client.
        /// </summary>
        public async Task<bool> FlagClientForReviewAsync(int clientId, int appointmentId, string reason)
        {
            try
            {
                // Check if client exists
                bool clientExists = await _context.Clients.AnyAsync(c => c.Id == clientId);
                if (!clientExists)
                {
                    _logger.LogWarning("Cannot flag client ID {ClientId} for review - client not found", clientId);
                    return false;
                }

                // Check if appointment exists
                bool appointmentExists = await _context.Appointments.AnyAsync(a => a.Id == appointmentId);
                if (!appointmentExists)
                {
                    _logger.LogWarning("Cannot flag client ID {ClientId} for review - appointment ID {AppointmentId} not found", 
                        clientId, appointmentId);
                    return false;
                }

                // Check for existing flag with the same reason and appointment
                var existingFlag = await _context.ClientReviewFlags
                    .Where(rf => rf.ClientId == clientId && 
                           rf.AppointmentId == appointmentId && 
                           rf.Status == "Pending")
                    .FirstOrDefaultAsync();

                if (existingFlag != null)
                {
                    // Update existing flag instead of creating a new one
                    existingFlag.AutoFlags += 1;
                    existingFlag.FlagDate = DateTimeOffset.UtcNow; // Update flag date
                    
                    _logger.LogInformation("Updated existing review flag for client ID {ClientId}, appointment ID {AppointmentId}. Flag count: {Count}", 
                        clientId, appointmentId, existingFlag.AutoFlags);
                }
                else
                {
                    // Create new flag
                    var reviewFlag = new ClientReviewFlag
                    {
                        ClientId = clientId,
                        AppointmentId = appointmentId,
                        FlagReason = reason,
                        FlagDate = DateTimeOffset.UtcNow,
                        Status = "Pending",
                        AutoFlags = 1
                    };

                    _context.ClientReviewFlags.Add(reviewFlag);
                    _logger.LogInformation("Created new review flag for client ID {ClientId}, appointment ID {AppointmentId}", 
                        clientId, appointmentId);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flagging client ID {ClientId} for review", clientId);
                return false;
            }
        }

        /// <summary>
        /// Updates the status of a client review flag.
        /// </summary>
        public async Task<bool> UpdateClientReviewFlagStatusAsync(int flagId, string status, string reviewedBy, string comments)
        {
            try
            {
                var flag = await _context.ClientReviewFlags.FindAsync(flagId);
                if (flag == null)
                {
                    _logger.LogWarning("Cannot update review flag ID {FlagId} - flag not found", flagId);
                    return false;
                }

                // Validate status
                if (status != "Approved" && status != "Rejected" && status != "Banned" && status != "Pending")
                {
                    _logger.LogWarning("Invalid status {Status} for review flag ID {FlagId}", status, flagId);
                    return false;
                }

                flag.Status = status;
                flag.ReviewedBy = reviewedBy;
                flag.ReviewDate = DateTimeOffset.UtcNow;
                flag.AdminComments = comments;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated review flag ID {FlagId} to status {Status}", flagId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review flag ID {FlagId}", flagId);
                return false;
            }
        }

        /// <summary>
        /// Bans or unbans a client from making appointments.
        /// </summary>
        public async Task<bool> UpdateClientBanStatusAsync(int clientId, bool isBanned, string reason, string adminName, string comments)
        {
            try
            {
                // Check if client exists
                bool clientExists = await _context.Clients.AnyAsync(c => c.Id == clientId);
                if (!clientExists)
                {
                    _logger.LogWarning("Cannot update ban status for client ID {ClientId} - client not found", clientId);
                    return false;
                }

                // Check for existing ban flags
                var existingFlag = await _context.ClientReviewFlags
                    .Where(rf => rf.ClientId == clientId && (rf.Status == "Banned" || rf.Status == "Rejected"))
                    .OrderByDescending(rf => rf.FlagDate)
                    .FirstOrDefaultAsync();

                if (existingFlag != null && isBanned)
                {
                    // Update existing flag to banned status
                    existingFlag.Status = "Banned";
                    existingFlag.FlagReason = reason ?? "Manual ban by administrator";
                    existingFlag.ReviewedBy = adminName ?? "Admin";
                    existingFlag.ReviewDate = DateTimeOffset.UtcNow;
                    existingFlag.AdminComments = comments;
                    
                    _logger.LogInformation("Updated existing flag to ban client ID {ClientId}", clientId);
                }
                else if (existingFlag != null && !isBanned)
                {
                    // Update existing flag to approved status to unban
                    existingFlag.Status = "Approved";
                    existingFlag.ReviewedBy = adminName ?? "Admin";
                    existingFlag.ReviewDate = DateTimeOffset.UtcNow;
                    existingFlag.AdminComments = comments ?? "Ban removed by administrator";
                    
                    _logger.LogInformation("Removed ban for client ID {ClientId}", clientId);
                }
                else if (isBanned)
                {
                    // Create new ban flag
                    var reviewFlag = new ClientReviewFlag
                    {
                        ClientId = clientId,
                        AppointmentId = 0, // No specific appointment for manual bans
                        FlagReason = reason ?? "Manual ban by administrator",
                        FlagDate = DateTimeOffset.UtcNow,
                        Status = "Banned",
                        ReviewedBy = adminName ?? "Admin",
                        ReviewDate = DateTimeOffset.UtcNow,
                        AdminComments = comments,
                        AutoFlags = 1
                    };

                    _context.ClientReviewFlags.Add(reviewFlag);
                    _logger.LogInformation("Created new ban flag for client ID {ClientId}", clientId);
                }
                else
                {
                    // No action needed - client is not banned
                    _logger.LogInformation("No action needed - client ID {ClientId} is not currently banned", clientId);
                    return true;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ban status for client ID {ClientId}", clientId);
                return false;
            }
        }
    }
}
