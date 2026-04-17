using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motivation.Core.Interfaces;
using Motivation.Data;
using Motivation.Models;
using Motivation.ViewModels;
using Newtonsoft.Json;

namespace Motivation.Controllers
{
    [Authorize(Roles = "Admins")]
    public class DepartmentsController : Controller
    {
        private readonly ILogger<RanksController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IRepository<Department> _departmentsRepository;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly IRepository<EmployeePenalty> _employeePenaltiesRepository;
        private readonly ISalaryCalculator _salaryCalculator;
        private readonly IEfficiencyCalculator _efficiencyCalculator;

        public DepartmentsController(
            ILogger<RanksController> logger,
            UserManager<IdentityUser> userManager,
            IRepository<Department> departmentsRepository,
            IEmployeesRepository employeesRepository,
            IRepository<EmployeePenalty> employeePenaltiesRepository,
            ISalaryCalculator salaryCalculator,
            IEfficiencyCalculator efficiencyCalculator
        )
        {
            _logger = logger;
            _userManager = userManager;
            _departmentsRepository = departmentsRepository;
            _employeesRepository = employeesRepository;
            _employeePenaltiesRepository = employeePenaltiesRepository;
            _salaryCalculator = salaryCalculator;
            _efficiencyCalculator = efficiencyCalculator;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var departments = await _departmentsRepository.Entries.ToListAsync();
            var departmentViewModels = new List<DepartmentViewModel>();
            foreach (var department in departments)
            {
                var childrenDepartments = await _departmentsRepository
                    .Entries.Where(d => d.ParentId == department.Id && d.Id != d.ParentId)
                    .ToListAsync();
                var employees = await _employeesRepository
                    .Entries.Where(e => e.DepartmentId == department.Id)
                    .ToListAsync();
                var manager = employees.FirstOrDefault(e => e.IsManager);
                int averageRank = (int)(employees.Average(e => e.Rank?.Number) ?? 0);
                var efficiency =
                    await _efficiencyCalculator.CalculateForDepartmentForCurrentMonthAsync(
                        department.Id
                    );
                decimal expenses = employees
                    .Select(async e => await _salaryCalculator.CalculateSalaryForCurrentMonth(e))
                    .Sum(e => e.Result);
                department.Expenses = expenses;

                var employeesIds = employees.Select(e => e.Id).ToHashSet();
                var penaltiesCount = await _employeePenaltiesRepository
                    .Entries.Where(p => employeesIds.Contains(p.EmployeeId))
                    .CountAsync();

                var departmentViewModel = new DepartmentViewModel
                {
                    ChildrenDepartments = childrenDepartments,
                    Department = department,
                    Employees = employees,
                    PenaltiesCount = penaltiesCount,
                    Efficiency = efficiency,
                    Manager = manager,
                    AverageRank = averageRank,
                };

                departmentViewModels.Add(departmentViewModel);
            }
            ;

            var departmentsViewModel = new DepartmentsViewModel
            {
                Departments = departmentViewModels.OrderBy(d => d.Department?.Id).ToList(),
            };

            return View(departmentsViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var departments = await _departmentsRepository.Entries.ToListAsync();
            var employees = await _employeesRepository.Entries.ToListAsync();
            var addDepartmentViewModel = new AddDepartmentViewModel
            {
                Departments = departments,
                Employees = employees,
            };
            return View(addDepartmentViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Add(Department department)
        {
            if (department == null)
                return StatusCode(500);
            await _departmentsRepository.CreateAsync(department);
            return Redirect("/Departments");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var department = await _departmentsRepository
                .Entries.Where(d => d.Id == id)
                .FirstOrDefaultAsync();
            if (department == null)
                return StatusCode(500);

            var departments = await _departmentsRepository
                .Entries.Where(d => d.Id != id && d.ParentId != id)
                .ToListAsync();
            var employees = await _employeesRepository
                .Entries.Where(e => e.DepartmentId == id)
                .ToListAsync();
            var editDepartmentViewModel = new EditDepartmentViewModel
            {
                Department = department,
                Departments = departments,
                Employees = employees,
            };
            return View(editDepartmentViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Department department)
        {
            if (department == null)
                return StatusCode(500);
            var employees = await _employeesRepository
                .Entries.Where(e => e.DepartmentId == department.Id)
                .ToListAsync();
            foreach (var employee in employees)
            {
                var user = await _userManager.FindByEmailAsync(employee.Email);
                if (employee.Id == department.ManagerId)
                {
                    employee.IsManager = true;
                    if (user != null)
                    {
                        await _userManager.AddToRoleAsync(user, "Managers");
                    }
                }
                else
                {
                    employee.IsManager = false;
                    if (user != null)
                    {
                        await _userManager.RemoveFromRoleAsync(user, "Managers");
                    }
                }
            }
            await _employeesRepository.UpdateRange(employees);
            await _departmentsRepository.UpdateAsync(department);
            return Redirect("/Departments");
        }

        [HttpDelete]
        public async Task Delete()
        {
            var department = await Request.ReadFromJsonAsync<Department>();
            var departmentId = department?.Id ?? -1;
            if (departmentId <= 0)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(
                    new { message = $"Нет такого подразделения id:{departmentId}!" }
                );
                await Response.WriteAsync(json);
            }
            await _departmentsRepository.DeleteAsync(departmentId);
        }
    }
}
