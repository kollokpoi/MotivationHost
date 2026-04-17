using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Motivation.Data;

namespace Motivation.Models
{
    public class BitrixPortal : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required, Url]
        public string PortalUrl { get; set; } = string.Empty;

        [Required]
        public string WebhookUrl { get; set; } = string.Empty;

        public string? IncomingSecret { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastSyncAt { get; set; }

        [NotMapped]
        public List<Department>? Departments { get; set; }

        [NotMapped]
        public List<Employee>? Employees { get; set; }

        [NotMapped]
        public List<EmployeeTask>? Tasks { get; set; }
    }
}
