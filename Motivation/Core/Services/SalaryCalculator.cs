using Microsoft.EntityFrameworkCore;
using Motivation.Core.Interfaces;
using Motivation.Data;
using Motivation.Models;

namespace Motivation.Core.Services
{
    public class SalaryCalculator : ISalaryCalculator
    {
        private const int MaxHours = 8;
        private readonly IRepository<Shift> _shiftsRepository;

        public SalaryCalculator(IRepository<Shift> shiftsRepository)
        {
            _shiftsRepository = shiftsRepository;
        }

        public async Task<decimal> CalculateSalaryForCurrentMonth(Employee employee) => await CalculateSalaryForMonth(employee, DateTime.Now.Year, DateTime.Now.Month);

        public async Task<int> CalculateWorkingTimeForCurrentMonth(Employee employee) => await CalculateWorkingTimeForMonth(employee, DateTime.Now.Year, DateTime.Now.Month);

        public async Task<decimal> CalculateSalaryForMonth(Employee employee, int year, int month)
        {
            if (employee == null) return 0;
            var workingTime = await CalculateWorkingTimeForMonth(employee, year, month);
            var salary = (employee.Position?.Salary + employee.Rank?.SalaryBonus) * workingTime ?? 0;
            return salary;
        }

        public async Task<int> CalculateWorkingTimeForMonth(Employee employee, int year, int month)
        {
            var shifts = await _shiftsRepository.Entries
               .Where(s => s.EmployeeId == employee.Id)
               .Where(s => s.Started.Year == year && s.Started.Month == month).ToListAsync();

            shifts = shifts
                .GroupBy(s => s.Started.Date)
                .Select(group =>
                    {
                        var first = group.OrderBy(s => s.Id).First();
                        var last = group.OrderByDescending(s => s.Id).First();

                        return new Shift
                        {
                            Id = first.Id,
                            EmployeeId = first.EmployeeId,
                            Employee = first.Employee,
                            Started = first.Started,
                            Ended = last.Ended,
                            LastPauseStart = last.LastPauseStart,
                            PauseTime = last.PauseTime,
                            LegalStartTime = first.LegalStartTime,
                            LegalEndTime = first.LegalEndTime,
                        };
                    })
                .Where(s => s.Ended != DateTime.MinValue) // Safety filter against workers who don't close the workday
                .ToList();

            var workingTime = shifts.Sum(s =>
            {
                var workMinutes = (int)Math.Floor((s.Ended - s.Started - s.PauseTime).TotalMinutes);
                var workHours = workMinutes / 60 + (workMinutes % 60 >= 45 ? 1 : 0);
                var legalHours = Math.Min(workHours, (int)(s.LegalEndTime - s.LegalStartTime).TotalHours);
                return Math.Min(legalHours, MaxHours);
            });
            
            return workingTime;
        }
    }
}
