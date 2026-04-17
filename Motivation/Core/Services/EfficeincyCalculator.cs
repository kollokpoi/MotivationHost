using Microsoft.EntityFrameworkCore;
using Motivation.Core.Interfaces;
using Motivation.Data;
using Motivation.Models;

namespace Motivation.Core.Services
{
    public class EfficeincyCalculator : IEfficiencyCalculator
    {
        private readonly IRepository<EmployeeTask> _employeeTasksRepository;
        private readonly IRepository<EmployeePenalty> _employeePenaltiesRepository;

        public EfficeincyCalculator(IRepository<EmployeeTask> employeeTasksRepository, 
            IRepository<EmployeePenalty> employeePenaltiesRepository)
        {
            _employeeTasksRepository = employeeTasksRepository;
            _employeePenaltiesRepository = employeePenaltiesRepository;
        }

        public async Task<int> CalculateForEmployeeForPreviousMonthAsync(int employeeId)
        {
            var previousPeriod = DateTime.Now.AddMonths(-1);
            var efficiency = await CalculateForEmployee(employeeId, previousPeriod.Year, previousPeriod.Month);
            return efficiency;
        }

        public async Task<int> CalculateForEmployeeForCurrentMonthAsync(int employeeId)
        {
            var efficiency = await CalculateForEmployee(employeeId, DateTime.Now.Year, DateTime.Now.Month);
            return efficiency;
        }
      
        public async Task<int> CalculateForDepartmentForPreviousMonthAsync(int departmentId)
        {
            var previousPeriod = DateTime.Now.AddMonths(-1);
            var efficiency = await CalculateForDepartment(departmentId, previousPeriod.Year, previousPeriod.Month);
            return efficiency;
        }

        public async Task<int> CalculateForDepartmentForCurrentMonthAsync(int departmentId)
        {
            var efficiency = await CalculateForDepartment(departmentId, DateTime.Now.Year, DateTime.Now.Month);
            return efficiency;
        }

        public async Task<int> CalculateForEmployee(int employeeId, int year, int month)
        {
            var penalties = _employeePenaltiesRepository.Entries.Where(p => p.EmployeeId == employeeId && p.Created.Year == year && p.Created.Month == month);
            var tasks = _employeeTasksRepository.Entries.Where(t => t.EmployeeId == employeeId && t.Created.Year == year && t.Created.Month == month);
            var efficiency = await Getfficiency(penalties, tasks);
            return efficiency;
        }

        public async Task<int> CalculateForDepartment(int departmentId, int year, int month)
        {
            var penalties = _employeePenaltiesRepository.Entries.Where(p => p.Employee.DepartmentId == departmentId && p.Created.Year == year && p.Created.Month == month);
            var tasks = _employeeTasksRepository.Entries.Where(t => t.Employee.DepartmentId == departmentId && t.Created.Year == year && t.Created.Month == month);
            var efficiency = await Getfficiency(penalties, tasks);
            return efficiency;
        }

        private async Task<int> Getfficiency(IQueryable<EmployeePenalty> penalties, IQueryable<EmployeeTask> tasks)
        {
            var penaltiesCount = await penalties.CountAsync();
            var tasksCount = await tasks.CountAsync();
            var lateCompletionTasksCount = await tasks.CountAsync(t => t.Ended > t.Deadline);
            var efficiency = CalculateEfficiency(tasksCount, penaltiesCount, lateCompletionTasksCount);
            return efficiency;
        }

        private int CalculateEfficiency(int tasksCount, int penaltiesCount, int lateCompletionTasksCount)
        {
            int efficiency = 100;
            if (tasksCount > 0)
            {
                var lateEfficiency = 1 - lateCompletionTasksCount / tasksCount;
                var penaltyEfficiency = 1 - penaltiesCount / tasksCount;
                efficiency = 100 * (lateEfficiency + penaltyEfficiency) / 2;
            }
            return efficiency;
        }
    }
}
