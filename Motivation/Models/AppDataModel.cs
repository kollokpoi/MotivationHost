namespace Motivation.Models
{
    public class AppDataModel
    { 
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string UpdateTitle { get; set; } = string.Empty;
        public string UpdateDetails { get; set; } = string.Empty;
        public string UpdateUrl { get; set; } = string.Empty;
    }
}
