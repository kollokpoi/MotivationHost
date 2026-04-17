using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Motivation.Data;

namespace Motivation.Models
{
    public class Penalty : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int PositionId { get; set; }

        [ForeignKey(nameof(PositionId))]
        public Position? Position { get; set; }

        public int Points { get; set; }
        public string Description { get; set; } = string.Empty;       
    }
}
