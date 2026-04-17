using Microsoft.EntityFrameworkCore;
using Motivation.Core.Interfaces;
using Motivation.Data;
using Motivation.Models;
using Motivation.Models.Mobile;

namespace Motivation.Controllers.MobileApi
{
    public interface IDepartmentGetter
    {
        Task<object> GetDepartmentObjectAsync(int departmentId);
        Task<object> GetDepartmentObjectForEmployeeAsync(int departmentId);
    }

    public class DepartmentGetter : IDepartmentGetter
    {
        private readonly ILogger<DepartmentGetter> _logger;
        private readonly IRepository<Department> _departmentsRepository;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly IRepository<EmployeePenalty> _employeePenaltiesRepository;
        private readonly ISalaryCalculator _salaryCalculator;
        private readonly IEfficiencyCalculator _efficiencyCalculator;

        public DepartmentGetter(ILogger<DepartmentGetter> logger,
            IRepository<Department> departmentsRepository,
            IEmployeesRepository employeesRepository,
            IRepository<EmployeePenalty> employeePenaltiesRepository,
            ISalaryCalculator salaryCalculator,
            IEfficiencyCalculator efficiencyCalculator)
        {
            _logger = logger;
            _departmentsRepository = departmentsRepository;
            _employeesRepository = employeesRepository;
            _employeePenaltiesRepository = employeePenaltiesRepository;
            _salaryCalculator = salaryCalculator;
            _efficiencyCalculator = efficiencyCalculator;
        }

        public async Task<object> GetDepartmentObjectForEmployeeAsync(int departmentId)
        {
            var month = DateTime.Now.Month;
            var year = DateTime.Now.Year;
            var startPeriod = new DateTime(year, month, 1);
            var endPeriod = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            var department = await _departmentsRepository.Entries.Where(d => d.Id == departmentId).FirstOrDefaultAsync();

            var employees = await _employeesRepository.Entries.Where(e => e.DepartmentId == departmentId).ToListAsync();

            var totalSalary = employees.Select(async e => await _salaryCalculator.CalculateSalaryForCurrentMonth(e)).Sum(d => d.Result);
            var totalWorkingTime = employees.Select(async e => await _salaryCalculator.CalculateWorkingTimeForCurrentMonth(e)).Sum(d => d.Result);

            int averageRank = (int)employees.Average(e => e.Rank?.Number ?? 0);
            var averageEfficiency = await _efficiencyCalculator.CalculateForDepartmentForCurrentMonthAsync(departmentId);

            var penalties = _employeePenaltiesRepository.Entries.Where(p => p.Employee.DepartmentId == departmentId && p.Created.Year == year && p.Created.Month == month);
            var penaltiesCount = penalties.Count();

            var photos = employees.Where(e => !e.IsManager).Select(e => e.Photo).ToList();

            var dep = new
            {
                id = departmentId,
                name = department?.Name,
                information = new
                {
                    budget = totalSalary.ToString("C2"),
                    rank = averageRank,
                    efficiency = averageEfficiency,
                    penalties = penaltiesCount
                },
                workers = new
                {
                    workers_count = photos.Count,
                    profile_photo = photos
                }
            };

            return dep;
        }

        public async Task<object> GetDepartmentObjectAsync(int departmentId)
        {
            var month = DateTime.Now.Month;
            var year = DateTime.Now.Year;
            var startPeriod = new DateTime(year, month, 1);
            var endPeriod = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            var previousStartPeriod = startPeriod.AddMonths(-1);

            var department = await _departmentsRepository.Entries.Where(d => d.Id == departmentId).FirstOrDefaultAsync();

            var employees = await _employeesRepository.Entries.Where(e => e.DepartmentId == departmentId).ToListAsync();
            var manager = employees.FirstOrDefault(e => e.IsManager);

            var totalSalary = employees.Select(async e => await _salaryCalculator.CalculateSalaryForCurrentMonth(e)).Sum(d => d.Result);
            var totalWorkingTime = employees.Select(async e => await _salaryCalculator.CalculateWorkingTimeForCurrentMonth(e)).Sum(d => d.Result);

            int averageRank = (int)employees.Average(e => e.Rank?.Number ?? 0);
            var averageEfficiency = await _efficiencyCalculator.CalculateForDepartmentForCurrentMonthAsync(departmentId);
            var previousAverageEfficiency = await _efficiencyCalculator.CalculateForDepartmentForPreviousMonthAsync(departmentId);
            var efficiencyDeviation = averageEfficiency - previousAverageEfficiency;

            var previousPenalties = _employeePenaltiesRepository.Entries.Where(p => p.Employee.DepartmentId == departmentId && p.Created.Year == year && p.Created.Month == month);
            var penalties = _employeePenaltiesRepository.Entries.Where(p => p.Employee.DepartmentId == departmentId && p.Created.Year == previousStartPeriod.Year && p.Created.Month == previousStartPeriod.Month);
            var penaltiesCount = await penalties.CountAsync();
            var previousPenaltiesCount = await previousPenalties.CountAsync();
            var penaltiesDeviation = -penaltiesCount * 100;
            if (previousPenaltiesCount > 0)
            {
                penaltiesDeviation = (previousPenaltiesCount - penaltiesCount) / previousPenaltiesCount;
            }

            var photos = employees.Where(e => !e.IsManager).Select(e => e.Photo).ToList();

            var budgetCard = new 
            {
                expected = totalSalary.ToString("C2"),
                plan = department.Budget.ToString("C2"),
                deviation = (department.Budget - totalSalary).ToString("C2")
            };

            var penaltiesCard = new Card
            {
                Name = "penalties",
                Title = "Замечания",
                Content = penaltiesCount.ToString(),
                SortIndex = 1,
                Clickable = penaltiesCount > 0
            };

            var penaltiesDeviationCard = new Card
            {
                Name = "penaltiesDeviation",
                Title = $"{(penaltiesDeviation > 0 ? "Лучше" : "Хуже")} чем в прошлом месяце",

                Content = Math.Abs(penaltiesDeviation).ToString(),
                SortIndex = 2,
                Highlited = penaltiesDeviation >= 0 ? Highlated.Green : Highlated.Red
            };

            var efficiencyCard = new Card
            {
                Name = "efficiency",
                Title = "Эффективность",
                Content = averageEfficiency.ToString(),
                SortIndex = 3,
            };

            var efficiencyDeviationCard = new Card
            {
                Name = "efficiencyDeviation",
                Title = "Отклонение эффективности",
                Content = efficiencyDeviation.ToString(),
                SortIndex = 4,
                Highlited = efficiencyDeviation >= 0 ? Highlated.Green : Highlated.Red
            };

            var workingTimeCard = new Card
            {
                Name = "workingTime",
                Title = "Рабочее время",
                Content = totalWorkingTime.ToString(),
                SortIndex = 5,
            };

            var cards = new object[] { penaltiesCard, penaltiesDeviationCard, efficiencyCard, efficiencyDeviationCard, workingTimeCard };

            var dep = new
            {
                id = departmentId,
                name = department?.Name,
                manager = new
                {
                    id = manager?.Id,
                    full_name = manager?.GetFullName(),
                    position = manager?.Position?.Name,
                    email = manager?.Email,
                    photo = manager?.Photo,
                    rank = manager?.Rank?.Number,
                },
                month = new
                {
                    start = startPeriod.ToString("d"),
                    end = endPeriod.ToString("d")
                },
                budget = budgetCard,
                rank = averageRank.ToString(),
                information = cards,
                workers = new
                {
                    workers_count = photos.Count,
                    percent_workers = 0,
                    profile_photo = photos
                }
            };

            return dep;
        }
    }
}
