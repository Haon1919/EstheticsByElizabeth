using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class ContactSubmissionDto
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Phone { get; set; }

        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [StringLength(100)]
        public string? InterestedService { get; set; }

        [StringLength(50)]
        public string? PreferredContactMethod { get; set; } = "Email";
    }
}
