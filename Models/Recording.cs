using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models
{
    [Table("Recordings")]
    public class Recording
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string RecordingType { get; set; } = string.Empty; // "recording" or "screenshot"

        [Required]
        [MaxLength(100)]
        public string MeetingCode { get; set; } = string.Empty;

        // Optional: Link to specific CallSession for session-based recordings
        public Guid? SessionId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("SessionId")]
        public virtual CallSession? CallSession { get; set; }

        // Additional metadata (JSON format)
        [MaxLength(1000)]
        public string? Metadata { get; set; }

        // File duration for video recordings (in seconds)
        public int? Duration { get; set; }

        // Thumbnail URL for video recordings
        [MaxLength(500)]
        public string? ThumbnailUrl { get; set; }

        // Tags for categorization
        [MaxLength(500)]
        public string? Tags { get; set; }

        // File description or notes
        [MaxLength(1000)]
        public string? Description { get; set; }

        // Download count for analytics
        public int DownloadCount { get; set; } = 0;

        // Last accessed timestamp
        public DateTime? LastAccessedAt { get; set; }
    }
} 