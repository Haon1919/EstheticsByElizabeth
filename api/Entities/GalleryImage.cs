using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities
{
    [Table("GalleryImages")]
    public class GalleryImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Src { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Alt { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
