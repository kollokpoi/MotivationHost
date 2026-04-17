using Motivation.Models;

namespace Motivation.ViewModels
{
    public class EmployeeViewModel
    {
        public Employee? Employee { get; set; }
        public decimal Salary { get; set; }
        public decimal SalaryPerHour { get; set; }
    }

    public class AddEmployeeViewModel
    {
        public List<Department> Departments { get; set; } = new List<Department>();
        public List<Position> Positions { get; set; } = new List<Position>();
    }

    public class EditEmployeeViewModel
    {
        public Employee? Employee { get; set; }
        public List<Department> Departments { get; set; } = new List<Department>();
        public List<Position> Positions { get; set; } = new List<Position>();
        public List<Qualification> Qualifications { get; set; } = new List<Qualification>();
        public List<Rank> Ranks { get; set; } = new List<Rank>();
    }

    public class EmployeesViewModel
    {
        public List<EmployeeViewModel> Employees { get; set; } = new List<EmployeeViewModel>();
        public List<Position> Positions { get; set; } = new List<Position>();
    }
}
