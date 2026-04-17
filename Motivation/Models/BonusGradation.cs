using Motivation.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Motivation.Models
{
    public class BonusGradation : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Price { get; set; }

        public int BonusId { get; set; }
        [ForeignKey("BonusId")]
        public Bonus? Bonus { get; set; }

    }
}
