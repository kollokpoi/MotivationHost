using System.ComponentModel.DataAnnotations;
using Motivation.Data;

namespace Motivation.Models
{
    public class PointOfInterest : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public override string ToString()
        {
            return $"Id:{Id:D}; Name:{Name};";
        }
    }
}
