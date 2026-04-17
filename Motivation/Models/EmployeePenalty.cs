using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Motivation.Data;

namespace Motivation.Models
{
    public class EmployeePenalty : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        public int AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public Employee? Author { get; set; }

        public int PenaltyId { get; set; }

        [ForeignKey("PenaltyId")]
        public Penalty? Penalty { get; set; }

        public string Explanation { get; set; } = string.Empty;
    }
}
