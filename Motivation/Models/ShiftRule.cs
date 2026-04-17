using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Motivation.Data;

namespace Motivation.Models
{
    public class ShiftRule : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        public DateTime StartTime{ get; set; }
        public DateTime EndTime { get; set; }
    }
}
