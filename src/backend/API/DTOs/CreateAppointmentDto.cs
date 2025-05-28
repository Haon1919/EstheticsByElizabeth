using System;
using System.Security.Cryptography;
using System.Text;

namespace API.DTOs
{
    public class CreateAppointmentDto
    {
        public int ServiceId { get; set; }
        public DateTimeOffset Time { get; set; }
        public ClientDto? Client { get; set; }

        /// <summary>
        /// Generates a deterministic idempotency key based on the appointment details
        /// This helps identify duplicate appointment requests even across retries
        /// </summary>
        /// <returns>A string that uniquely identifies this appointment request</returns>
        public string GetIdempotencyKey()
        {
            if (Client == null)
            {
                throw new InvalidOperationException("Client information is required for idempotency key generation");
            }

            // Create a string that combines all the unique properties that identify an appointment
            var keyComponents = $"{Client.Email.ToLowerInvariant()}|{ServiceId}|{Time.UtcDateTime:yyyy-MM-ddTHH:mm:ss}";
            
            // Create a hash of the key components to form a deterministic idempotency key
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyComponents));
            
            // Convert the hash to a string
            return Convert.ToBase64String(hashBytes);
        }
    }
}