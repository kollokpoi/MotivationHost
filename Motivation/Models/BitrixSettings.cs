using System.ComponentModel.DataAnnotations;

namespace Motivation.Models
{
    public class BitrixSettings
    {
        public int Id { get; set; }

        public bool Enabled { get; set; }

        [Required, Url]
        public string WebhookUrl { get; set; } = string.Empty;
        public string? EncryptedIncomingSecret { get; set; }

        public int SyncIntervalMinutes { get; set; } = 30;
        public bool SyncTasks { get; set; } = true;
        public bool SyncUsers { get; set; } = true;
        public bool SyncDepartments { get; set; } = true;
        public bool SyncDeals { get; set; } = false;
        public bool TwoWaySync { get; set; } = false;

        public DateTime? LastSyncAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
