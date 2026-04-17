using Motivation.Models;
using Motivation.Data.Repositories;

namespace Motivation.ViewModels
{
    public class EmployeePenaltyViewModel
    {
        public EmployeePenalty? EmployeePenalty { get; set; }
    }

    public class EmployeePenaltiesViewModel
    {
        public List<EmployeePenaltyViewModel> EmployeePenalties { get; set; } = new List<EmployeePenaltyViewModel>();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class AddEmployeePenaltyViewModel
    {
        public List<Employee> Employees { get; set; } = new List<Employee>();
        public List<Penalty> Penalties { get; set; } = new List<Penalty>();
    }

    public class EditEmployeePenaltyViewModel
    {
        public EmployeePenalty? EmployeePenalty { get; set; }
        public List<Employee> Employees { get; set; } = new List<Employee>();
        public List<Penalty> Penalties { get; set; } = new List<Penalty>();
    }
}
