using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities // Replace YourProjectName with your actual project namespace
{
    public class Appointment
    {
        [Key]
        [Column("id")] // Adding column name mapping to match database column name
        public int Id { get; set; }

        [Required] // Foreign key property
        [Column("clientid")]
        public int ClientId { get; set; }

        [Required] // Foreign key property
        [Column("serviceid")]
        public int ServiceId { get; set; }

        [Required] // Corresponds to NOT NULL in SQL
        [Column("time")] // Adding column name mapping to match database column name
        // This should be TIMESTAMPTZ as per SQL
        public DateTimeOffset Time { get; set; }

        // --- Navigation Properties for Relationships ---

        // Reference navigation property back to the Client (many-to-one)
        [ForeignKey("ClientId")]
        public virtual Client Client { get; set; }

        // Reference navigation property back to the Service (many-to-one)
        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; }
    }
}