using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class CreateGalleryImageDto
    {
        [Required]
        [MaxLength(500)]
        public required string Src { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Alt { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Category { get; set; }

        [MaxLength(255)]
        public string? Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;
    }

    public class UpdateGalleryImageDto
    {
        [MaxLength(500)]
        public string? Src { get; set; }

        [MaxLength(255)]
        public string? Alt { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }

        [MaxLength(255)]
        public string? Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }

        public int? SortOrder { get; set; }
    }

    public class GalleryImageResponseDto
    {
        public int Id { get; set; }
        public required string Src { get; set; }
        public required string Alt { get; set; }
        public required string Category { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public DateTimeOffset UploadedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public class GalleryImageListResponseDto
    {
        public bool Success { get; set; } = true;
        public required List<GalleryImageResponseDto> Data { get; set; }
        public int TotalCount { get; set; }
        public required List<string> Categories { get; set; }
    }

    public class GalleryCategoryDto
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public int Count { get; set; }
    }

    public class UploadImageResponseDto
    {
        public bool Success { get; set; }
        public required string Url { get; set; }
        public required string Filename { get; set; }
        public string? Message { get; set; }
    }

    public class ReorderImagesDto
    {
        public required List<int> ImageIds { get; set; }
    }
}
