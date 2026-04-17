using Motivation.Models;

namespace Motivation.ViewModels
{
    public class RanksViewModel
    {
        public List<Position> Positions { get; set; } = new List<Position>();
        public List<Rank> Ranks { get; set; } = new List<Rank>();
    }
}
