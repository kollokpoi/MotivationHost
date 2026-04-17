using Motivation.Models;

namespace Motivation.ViewModels
{
    public class AddDepartmentViewModel
    {
        public List<Department> Departments { get; set; } = new List<Department>();
        public List<Employee> Employees { get; set; } = new List<Employee>();
    }

    public class EditDepartmentViewModel
    {
        public Department? Department { get; set; }
        public List<Department> Departments { get; set; } = new List<Department>();
        public List<Employee> Employees { get; set; } = new List<Employee>();
    }

    public class DepartmentViewModel
    {
        public Department? Department { get; set; }
        public List<Department> ChildrenDepartments { get; set; } = new List<Department>();
        public List<Employee> Employees { get; set; } = new List<Employee>();
        public Employee? Manager { get; set; }
        public int Efficiency { get; set; }
        public int PenaltiesCount { get; set; }
        public int AverageRank { get; set; }
    }

    public class DepartmentsViewModel
    {
        public List<DepartmentViewModel> Departments { get; set; } = new List<DepartmentViewModel>();
    }
}
