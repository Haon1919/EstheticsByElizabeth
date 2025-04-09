using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities // Replace YourProjectName with your actual project namespace
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required] // Foreign key property
        public int ClientId { get; set; }

        [Required] // Foreign key property
        public int ServiceId { get; set; }

        [Required] // Corresponds to NOT NULL in SQL
        // This should be TIMESTAMPTZ as per SQL
        public DateTimeOffset Time { get; set; }

        // --- Navigation Properties for Relationships ---

        // Reference navigation property back to the Client (many-to-one)
        [ForeignKey("ClientId")]
        public virtual Client? Client { get; set; }

        // Reference navigation property back to the Service (many-to-one)
        [ForeignKey("ServiceId")]
        public virtual Service? Service { get; set; }
    }
}