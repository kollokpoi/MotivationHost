using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Motivation.Data;

namespace Motivation.Models
{
    public class Rank : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int PositionId { get; set; }

        [ForeignKey(nameof(PositionId))]
        public Position? Position { get; set; }

        public int Number { get; set; }
        public decimal SalaryBonus { get; set; }
    }
}
