using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motivation.Core.Interfaces;
using Motivation.Data;
using Motivation.Models;
using Motivation.ViewModels;
using NuGet.Packaging;

namespace Motivation.Controllers
{
    [Authorize(Roles = "Admins, Managers")]
    [Route("[controller]")]
    public class WagesController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<WagesController> _logger;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly ISalaryCalculator _salaryCalculator;
        private readonly IRepository<EmployeeTask> _employeeTasksRepository;
        private readonly IRepository<Bonus> _bonusRepository;
        private readonly IRepository<EmployeeBonus> _employeeBonusRepository;


        public WagesController(
            UserManager<IdentityUser> userManager,
            ILogger<WagesController> logger,
            ISalaryCalculator salaryCalculator,
            IEmployeesRepository employeesRepository,
            IRepository<EmployeeTask> employeeTasksRepository,
            IRepository<Bonus> bonusRepository,
            IRepository<EmployeeBonus> employeeBonusRepository)
        {
            _userManager = userManager;
            _logger = logger;
            _salaryCalculator = salaryCalculator;
            _employeesRepository = employeesRepository;
            _employeeTasksRepository = employeeTasksRepository;
            _bonusRepository = bonusRepository;
            _employeeBonusRepository = employeeBonusRepository;
        }

        public async Task<IActionResult> Index()
        {
            var employees = await _employeesRepository
                .Entries.Include(e => e.Department)
                .ToListAsync();
            return View(employees);
        }

        [HttpGet("get-data/{employeeId}")]
        public IActionResult GetEmployeeSalaryData(int employeeId)
        {
            var employee = _employeesRepository.Entries.FirstOrDefault(x => x.Id == employeeId);
            if (employee is null)
                return StatusCode(404);
            var tasks = _employeeTasksRepository.Entries.Where(t => t.EmployeeId == employeeId).ToList() ?? [];
            var positionBonuses = _bonusRepository.Entries.Where(x => x.PositionId == employee.PositionId).ToList() ?? [];
            var employeeBonuses = _employeeBonusRepository.Entries.Where(x => x.EmployeeId == employeeId).ToList() ?? [];

            var viewModel = new EmployeeWageViewModel
            {
                Tasks = tasks,
                Bonuses = positionBonuses,
                EmployeeBonuses = employeeBonuses
            };

            return PartialView("_WageData", viewModel);
        }

        [HttpPost("update-employee-bonus")]
        public async Task<IActionResult> UpdateEmployeeBonus([FromForm] int employeeId, [FromForm] int bonusId, [FromForm] int gradationId)
        {
            var existingBonus = _employeeBonusRepository.Entries
           .Include(eb => eb.BonusGradation)
           .FirstOrDefault(eb => eb.BonusGradation != null && eb.EmployeeId == employeeId && eb.BonusGradation.BonusId == bonusId);

            if (existingBonus != null)
                await _employeeBonusRepository.DeleteAsync(existingBonus.Id);

            await _employeeBonusRepository.CreateAsync(new EmployeeBonus
            {
                EmployeeId = employeeId,
                BonusGradationId = gradationId
            });

            var tasks = _employeeTasksRepository.Entries.Where(t => t.EmployeeId == employeeId).ToList();
            var employeeBonuses = _employeeBonusRepository.Entries
                .Where(x => x.EmployeeId == employeeId)
                .Include(eb => eb.BonusGradation)
                .ToList();

            return Json(new
            {
                bonusesTotal = employeeBonuses.Sum(eb => eb.BonusGradation?.Price ?? 0),
                grandTotal = tasks.Sum(t => t.Price) + employeeBonuses.Sum(eb => eb.BonusGradation?.Price ?? 0)
            });
        }
    }
}
