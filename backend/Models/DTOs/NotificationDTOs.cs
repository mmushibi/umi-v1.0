using System.ComponentModel.DataAnnotations;

namespace UmiHealthPOS.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? UserId { get; set; }
        public string? TenantId { get; set; }
        public bool IsRead { get; set; }
        public bool IsGlobal { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
        public string? Metadata { get; set; }
        public string Timestamp => NotificationHelper.GetRelativeTime(CreatedAt);
    }

    public class CreateNotificationRequest
    {
        [Required]
        [StringLength(100)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Category { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        public bool IsGlobal { get; set; } = false;

        public DateTime? ExpiresAt { get; set; }

        [StringLength(100)]
        public string? ActionUrl { get; set; }

        [StringLength(50)]
        public string? ActionText { get; set; }

        [StringLength(1000)]
        public string? Metadata { get; set; }
    }

    public class UpdateNotificationRequest
    {
        [Required]
        [StringLength(100)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Category { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [StringLength(100)]
        public string? ActionUrl { get; set; }

        [StringLength(50)]
        public string? ActionText { get; set; }

        [StringLength(1000)]
        public string? Metadata { get; set; }
    }

    public class NotificationStatsDto
    {
        public int TotalNotifications { get; set; }
        public int UnreadNotifications { get; set; }
        public int ReadNotifications { get; set; }
        public int GlobalNotifications { get; set; }
        public int UserNotifications { get; set; }
        public int TenantNotifications { get; set; }
        public int ExpiredNotifications { get; set; }
    }

    public class NotificationFilterDto
    {
        public string? Type { get; set; }
        public string? Category { get; set; }
        public bool? IsRead { get; set; }
        public bool? IsGlobal { get; set; }
        public string? Search { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class NotificationBatchRequest
    {
        public List<int> NotificationIds { get; set; } = new();
        public string Action { get; set; } = string.Empty; // "mark_read", "mark_unread", "delete"
    }

    public static class NotificationHelper
    {
        public static string GetRelativeTime(DateTime dateTime)
        {
            var now = DateTime.UtcNow;
            var span = now - dateTime;

            if (span.TotalSeconds < 60)
                return "Just now";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes} minute{((int)span.TotalMinutes != 1 ? "s" : "")} ago";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours} hour{((int)span.TotalHours != 1 ? "s" : "")} ago";
            if (span.TotalDays < 7)
                return $"{(int)span.TotalDays} day{((int)span.TotalDays != 1 ? "s" : "")} ago";
            if (span.TotalDays < 30)
                return $"{(int)(span.TotalDays / 7)} week{((int)(span.TotalDays / 7) != 1 ? "s" : "")} ago";
            if (span.TotalDays < 365)
                return $"{(int)(span.TotalDays / 30)} month{((int)(span.TotalDays / 30) != 1 ? "s" : "")} ago";

            return $"{(int)(span.TotalDays / 365)} year{((int)(span.TotalDays / 365) != 1 ? "s" : "")} ago";
        }
    }
}
