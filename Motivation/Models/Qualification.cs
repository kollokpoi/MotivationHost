using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Motivation.Data;

namespace Motivation.Models
{
    public class Qualification : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int PositionId { get; set; }

        [ForeignKey(nameof(PositionId))]
        public Position? Position { get; set; }

        public string Name { get; set; } = string.Empty;
        public int Points { get; set; }

        public override string ToString()
        {
            return $"Id: {Id:D}; Name: {Name}";
        }
    }
}
