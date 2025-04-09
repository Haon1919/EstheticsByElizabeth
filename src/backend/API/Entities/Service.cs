using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities // Replace YourProjectName with your actual project namespace
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        // Nullable because ON DELETE SET NULL
        public int? CategoryId { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        // Description corresponds to TEXT, so no StringLength needed, but MaxLength can be used if desired.
        // Nullable in the DB schema
        public string? Description { get; set; }

        // Duration is nullable INT in the DB schema
        public int? Duration { get; set; } // Nullable int

        [Required]
        [Column(TypeName = "decimal(10, 2)")] // Specify SQL data type for precision
        public decimal Price { get; set; }

        [StringLength(2048)]
        // Website is nullable in the DB schema
        public string? Website { get; set; }

        // --- Navigation Properties for Relationships ---

        // Reference navigation property back to the Category (many-to-one)
        // Represents the relationship: One Service belongs to one Category
        [ForeignKey("CategoryId")] // Links this navigation property to the FK property above
        public virtual Category? Category { get; set; } // Can be null if CategoryId was nullable (but it's not here)

        // Navigation property for related Appointments (one-to-many)
        // Represents the relationship: One Service can be part of many Appointments
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>(); // Initialize collection
    }
}