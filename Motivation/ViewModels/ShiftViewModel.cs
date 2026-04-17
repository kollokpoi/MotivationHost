using Motivation.Models;

namespace Motivation.ViewModels
{
    public class ShiftViewModel
    {
        public Employee Employee { get; set; } = new Employee();
        public List<int> WorkingHours { get; set; } = new List<int>();
        public List<string> Comments { get; set; } = new List<string>();
    }
}
