using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities
{
    public class ContactSubmission
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [EmailAddress]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(30)]
        [Column("phone")]
        public string? Phone { get; set; }

        [Required]
        [StringLength(100)]
        [Column("subject")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [StringLength(100)]
        [Column("interestedservice")]
        public string? InterestedService { get; set; }

        [StringLength(50)]
        [Column("preferredcontactmethod")]
        public string? PreferredContactMethod { get; set; } = "Email";

        [Required]
        [Column("submittedat")]
        public DateTimeOffset SubmittedAt { get; set; }

        [Required]
        [StringLength(20)]
        [Column("status")]
        public string Status { get; set; } = "unread";

        [Column("readat")]
        public DateTimeOffset? ReadAt { get; set; }

        [Column("respondedat")]
        public DateTimeOffset? RespondedAt { get; set; }

        [Column("adminnotes")]
        public string? AdminNotes { get; set; }
    }
}
