using System.Text.Json.Serialization;

namespace Motivation.Models.Mobile
{
    public class BitrixResponseTaskModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? DeadLine { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        [JsonPropertyName("UF_AUTO_359773460993")]
        public string? Price { get; set; } = string.Empty;
    }
}
