using Motivation.Models;

namespace Motivation.Data
{
    public interface IEmployeesRepository : IRepository<Employee>
    {
        Task UpdateEmployeeStatus(int employeeId, EmployeeStatus status);
        Task UpdateRange(IList<Employee> employees);
    }
}
