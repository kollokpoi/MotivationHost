using Motivation.Models;

namespace Motivation.ViewModels
{
    public class EmployeeWageViewModel
    {
        public List<EmployeeTask> Tasks { get; set; } = [];
        public List<Bonus> Bonuses { get; set; } = [];
        public List<EmployeeBonus> EmployeeBonuses { get; set; } = [];

    }
}
