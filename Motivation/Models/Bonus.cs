using Motivation.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Motivation.Models
{
    public class Bonus : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public int PositionId { get; set; }
        [ForeignKey("PositionId")]
        public Position? Position { get; set; }
        public List<BonusGradation> Gradations { get; set; } = [];
    }
}
