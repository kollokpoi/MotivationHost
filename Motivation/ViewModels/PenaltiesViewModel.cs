using Motivation.Models;

namespace Motivation.ViewModels
{
    public class PenaltiesViewModel
    {
        public List<Penalty> Penalties { get; set; } = new List<Penalty>();
        public List<Position> Positions { get; set; } = new List<Position>();
    }
}
