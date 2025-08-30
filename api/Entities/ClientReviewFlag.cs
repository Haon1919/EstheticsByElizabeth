using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities
{
    public class ClientReviewFlag
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("clientid")]
        public int ClientId { get; set; }

        [Required]
        [Column("appointmentid")]
        public int AppointmentId { get; set; }

        [Required]
        [Column("flagreason")]
        [StringLength(500)]
        public string FlagReason { get; set; } = string.Empty;

        [Required]
        [Column("flagdate")]
        public DateTimeOffset FlagDate { get; set; }

        [Column("reviewedby")]
        [StringLength(255)]
        public string? ReviewedBy { get; set; }

        [Column("reviewdate")]
        public DateTimeOffset? ReviewDate { get; set; }

        [Required]
        [Column("status")]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [Column("admincomments")]
        [StringLength(1000)]
        public string? AdminComments { get; set; }

        [Column("autoflags")]
        public int AutoFlags { get; set; } = 1; // Number of times auto-flagged

        // Navigation Properties
        [ForeignKey("ClientId")]
        public virtual Client Client { get; set; } = null!;

        [ForeignKey("AppointmentId")]
        public virtual Appointment Appointment { get; set; } = null!;
    }
}
