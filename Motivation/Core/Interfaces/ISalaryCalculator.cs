using Motivation.Models;

namespace Motivation.Core.Interfaces
{
    public interface ISalaryCalculator
    {
        Task<decimal> CalculateSalaryForMonth(Employee employee, int year, int month);
        Task<int> CalculateWorkingTimeForMonth(Employee employee, int year, int month);
        Task<decimal> CalculateSalaryForCurrentMonth(Employee employee);
        Task<int> CalculateWorkingTimeForCurrentMonth(Employee employee);
    }
}
