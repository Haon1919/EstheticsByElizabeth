using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace API.Entities
{
    public class Client
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        [Column("firstname")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [Column("lastname")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [EmailAddress]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        [Column("phonenumber")]
        public string? PhoneNumber { get; set; }

        [JsonIgnore]
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        [JsonIgnore]
        public virtual ICollection<ClientReviewFlag> ReviewFlags { get; set; } = new List<ClientReviewFlag>();
    }
}