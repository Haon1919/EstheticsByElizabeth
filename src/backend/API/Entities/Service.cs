using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace API.Entities // Replace YourProjectName with your actual project namespace
{
    public class Service
    {
        public Service()
        {
            // Initialize collection to avoid null reference exceptions
            Appointments = new HashSet<Appointment>();
        }

        [Key]
        [Column("id")]
        public int Id { get; set; }

        // Nullable because ON DELETE SET NULL
        [Column("categoryid")]
        public int? CategoryId { get; set; }

        [Required]
        [Column("name")]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        // Description corresponds to TEXT, so no StringLength needed, but MaxLength can be used if desired.
        // Nullable in the DB schema
        [Column("description")]
        public string? Description { get; set; }

        // Duration is nullable INT in the DB schema
        [Column("duration")]
        public int? Duration { get; set; } // Nullable int

        // Price is nullable in the DB schema
        [Column("price")] // Specify SQL data type for precision
        public decimal? Price { get; set; }

        [StringLength(2048)]
        // Website is nullable in the DB schema
        [Column("website")]
        public string? Website { get; set; }

        // --- Navigation Properties for Relationships ---

        // Reference navigation property back to the Category (many-to-one)
        // Represents the relationship: One Service belongs to one Category
        [ForeignKey("CategoryId")] // Links this navigation property to the FK property above
        public virtual Category? Category { get; set; } // Can be null if CategoryId was nullable (but it's not here)

        // Navigation property for related Appointments (one-to-many)
        // Represents the relationship: One Service can be part of many Appointments
        [JsonIgnore]
        public virtual ICollection<Appointment> Appointments { get; set; }
    }
}