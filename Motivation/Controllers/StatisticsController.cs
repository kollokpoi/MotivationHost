using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motivation.Data;
using Motivation.Models;
using Motivation.ViewModels;

namespace Motivation.Controllers
{
    [Authorize(Roles = "Admins, Managers")]
    public class StatisticsController : Controller
    {
        private readonly ILogger<StatisticsController> _logger;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly IRepository<Department> _departmentsRepository;
        private readonly IRepository<EmployeePenalty> _employeePenaltiesRepository;

        public StatisticsController(
            ILogger<StatisticsController> logger,
            IEmployeesRepository employeesRepository,
            IRepository<Department> departmentsRepository,
            IRepository<EmployeePenalty> employeePenaltiesRepository
        )
        {
            _logger = logger;
            _employeesRepository = employeesRepository;
            _departmentsRepository = departmentsRepository;
            _employeePenaltiesRepository = employeePenaltiesRepository;
        }

        public IActionResult Index()
        {
            var workersCount = _employeesRepository.Entries.Count();
            var budget = _departmentsRepository.Entries.Sum(d => d.Budget);
            var penaltiesCount = _employeePenaltiesRepository.Entries.Count();
            var monthPenaltiesCount = _employeePenaltiesRepository.Entries.Where(p => p.Created.Month == DateTime.UtcNow.Month).Count();
            var statistics = new StatisticsViewModel
            {
                Budget = budget,
                PenaltiesCount = penaltiesCount,
                WorkersCount = workersCount,
                MonthPenaltiesCount = monthPenaltiesCount
            };
            return View(statistics);
        }
    }
}
