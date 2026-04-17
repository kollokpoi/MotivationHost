using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Motivation.Data;

namespace Motivation.Models
{
    public class Shift : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        public DateTime Started { get; set; }
        public DateTime Ended { get; set; }
        public DateTime LastPauseStart { get; set; }
        public TimeSpan PauseTime { get; set; }
        public DateTime LegalStartTime { get; set; }
        public DateTime LegalEndTime { get; set; }
    }
}
