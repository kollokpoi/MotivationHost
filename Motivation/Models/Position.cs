using System.ComponentModel.DataAnnotations;
using Motivation.Data;

namespace Motivation.Models
{
    public class Position : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Salary { get; set; }

        public override string ToString()
        {
            return $"Id:{Id:D}; Name:{Name};";
        }
    }
}
