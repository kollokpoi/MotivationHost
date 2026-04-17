using Motivation.Models;

namespace Motivation.ViewModels
{
    public class SummarySheetViewModel
    {
        public List<ScoreSheet> ScoreSheets { get; set; } = new List<ScoreSheet>();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int BasePoints { get; set; }
    }
}
