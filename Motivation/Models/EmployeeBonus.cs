using Motivation.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Motivation.Models
{
    public class EmployeeBonus : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public int BonusGradationId { get; set; }
        [ForeignKey("BonusGradationId")]
        public BonusGradation? BonusGradation { get; set; }
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }
    }
}
