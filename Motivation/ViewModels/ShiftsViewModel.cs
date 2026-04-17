namespace Motivation.ViewModels
{
    public class ShiftsViewModel
    {
        public List<ShiftViewModel> EmployeesShifts { get; set; } = new List<ShiftViewModel>();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
