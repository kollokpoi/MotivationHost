using Motivation.Models;

namespace Motivation.ViewModels
{
    public class QualificationsViewModel
    {
        public List<Qualification> Qualifications { get; set; } = new List<Qualification>();
        public List<Position> Positions { get; set; } = new List<Position>();
    }
}
