using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Motivation.Data;

namespace Motivation.Models
{
    public class ScoreSheet : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        public int RankId { get; set; }

        [ForeignKey("RankId")]
        public Rank? Rank { get; set; }

        public int NewRankId { get; set; }

        [ForeignKey("NewRankId")]
        public Rank? NewRank { get; set; }

        public int CalculatedRankId { get; set; }

        [ForeignKey("CalculatedRankId")]
        public Rank? CalculatedRank { get; set; }

        public int QualificationId { get; set; }

        [ForeignKey("QualificationId")]
        public Qualification? Qualification { get; set; }

        public DateTime StartPeriod { get; set; }
        public DateTime EndPeriod { get; set; }

        public int Efficiency { get; set; }
        public double PenaltyPoints { get; set; }
        public int Score { get; set; }
        public int ShiftsCount { get; set; }
        public int WorkingTime { get; set; }
        public decimal Salary { get; set; }
        public bool? IsSigned { get; set; } = null;
    }
}
