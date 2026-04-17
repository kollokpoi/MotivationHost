using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Motivation.Data;

namespace Motivation.Models
{
    public enum TaskPriority
    {
        Low,
        Medium,
        High,
    }

    public enum TaskStatus
    {
        New,
        InProgress,
        Finished,
    }

    public class EmployeeTask : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int? PortalId { get; set; }

        [ForeignKey("PortalId")]
        public BitrixPortal? Portal { get; set; }

        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        public int AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public Employee? Author { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Started { get; set; }
        public DateTime Ended { get; set; }
        public DateTime? Deadline { get; set; }
        public int Score { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.New;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public decimal Cost { get; set; } = 0;
    }
}
