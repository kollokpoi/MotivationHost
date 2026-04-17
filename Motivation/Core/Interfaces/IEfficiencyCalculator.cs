namespace Motivation.Core.Interfaces
{
    public interface IEfficiencyCalculator
    {
        Task<int> CalculateForEmployee(int employeeId, int year, int month);
        Task<int> CalculateForDepartment(int departmentId, int year, int month);
        Task<int> CalculateForEmployeeForPreviousMonthAsync(int employeeId);
        Task<int> CalculateForEmployeeForCurrentMonthAsync(int employeeId);
        Task<int> CalculateForDepartmentForPreviousMonthAsync(int departmentId);
        Task<int> CalculateForDepartmentForCurrentMonthAsync(int departmentId);
    }
}
