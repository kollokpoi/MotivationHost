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
    [Authorize(Roles = "Admins, Managers")]
    public class EmployeesController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<EmployeesController> _logger;
        private readonly IWebHostEnvironment _appEnvironment;

        private readonly IRepository<Rank> _ranksRepository;
        private readonly IRepository<Position> _positionsRepository;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly IRepository<Department> _departmentsRepository;
        private readonly IRepository<Qualification> _qualificationRepository;
        private readonly ISalaryCalculator _salaryCalculator;

        public EmployeesController(
            ILogger<EmployeesController> logger,
            IWebHostEnvironment appEnvironment,
            IRepository<Rank> ranksRepository,
            IRepository<Position> positionsRepository,
            IEmployeesRepository employeesRepository,
            IRepository<Department> departmentsRepository,
            IRepository<Qualification> qualificationRepository,
            UserManager<IdentityUser> userManager,
            ISalaryCalculator salaryCalculator
        )
        {
            _logger = logger;
            _appEnvironment = appEnvironment;
            _ranksRepository = ranksRepository;
            _positionsRepository = positionsRepository;
            _employeesRepository = employeesRepository;
            _departmentsRepository = departmentsRepository;
            _qualificationRepository = qualificationRepository;
            _userManager = userManager;
            _salaryCalculator = salaryCalculator;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var employeeViewModels = new List<EmployeeViewModel>();
            var employees = await _employeesRepository
                .Entries.Include(e => e.Department)
                .ToListAsync();
            foreach (var employee in employees)
            {
                var salary = await _salaryCalculator.CalculateSalaryForCurrentMonth(employee);
                var employeeViewModel = new EmployeeViewModel
                {
                    Employee = employee,
                    Salary = salary,
                    SalaryPerHour = (employee.Position?.Salary + employee.Rank?.SalaryBonus) ?? 0,
                };
                employeeViewModels.Add(employeeViewModel);
            }
            var employeesViewModel = new EmployeesViewModel
            {
                Employees = employeeViewModels.OrderBy(e => e.Employee?.Id).ToList(),
            };
            return View(employeesViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var departments = await _departmentsRepository.Entries.ToListAsync();
            var positions = await _positionsRepository.Entries.ToListAsync();
            var addEmployeeViewModel = new AddEmployeeViewModel
            {
                Departments = departments,
                Positions = positions,
            };
            return View(addEmployeeViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Add(Employee employee)
        {
            if (employee == null)
                return StatusCode(500);
            if (employee.EndTime < employee.StartTime)
            {
                employee.EndTime = employee.EndTime.AddDays(1);
            }
            employee.StartTime = employee.StartTime.ToUniversalTime();
            employee.EndTime = employee.EndTime.ToUniversalTime();
            await UploadEmployeeImage(employee);

            var username = employee.Email.Split('@')[0];
            var user = new IdentityUser { UserName = username, Email = employee.Email };
            var password = employee.Password;
            await _userManager.CreateAsync(user, password);
            await _userManager.AddToRoleAsync(user, "Users");

            employee.UserId = user.Id;

            await _employeesRepository.CreateAsync(employee);

            return Redirect("/Employees");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _employeesRepository
                .Entries.Where(e => e.Id == id)
                .FirstOrDefaultAsync();
            if (employee == null)
                return StatusCode(500);

            var departments = await _departmentsRepository.Entries.ToListAsync();
            var positions = await _positionsRepository.Entries.ToListAsync();
            var qualifications = await _qualificationRepository
                .Entries.Where(q => q.PositionId == employee.PositionId)
                .ToListAsync();
            var ranks = await _ranksRepository
                .Entries.Where(r => r.PositionId == employee.PositionId)
                .ToListAsync();
            var editEmployeeViewModel = new EditEmployeeViewModel
            {
                Employee = employee,
                Departments = departments,
                Positions = positions,
                Qualifications = qualifications,
                Ranks = ranks,
            };
            return View(editEmployeeViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Employee employee)
        {
            if (employee == null)
                return StatusCode(500);
            employee.StartTime = employee.StartTime.ToUniversalTime();
            employee.EndTime = employee.EndTime.ToUniversalTime();
            var user = await _userManager.FindByEmailAsync(employee.Email);
            if (user != null)
            {
                var token = await _userManager.GenerateChangeEmailTokenAsync(user, employee.Email);
                await _userManager.ChangeEmailAsync(user, employee.Email, token);
                employee.UserId = user.Id;
                if (employee.Password != null)
                {
                    token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    await _userManager.ResetPasswordAsync(user, token, employee.Password);
                }
                await _employeesRepository.UpdateAsync(employee);
                await UploadEmployeeImage(employee);
            }
            return Redirect("/Employees");
        }

        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            var req = await Request.ReadFromJsonAsync<Employee>();
            if (req == null)
                return StatusCode(500);
            var employeeId = req.Id;
            var employee = _employeesRepository
                .Entries.Where(u => u.Id == employeeId)
                .FirstOrDefault();
            if (employee == null)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = "Нет такого сотрудника!" });
                await Response.WriteAsync(json);
                return BadRequest();
            }
            await _employeesRepository.DeleteAsync(employeeId);

            var user = await _userManager.FindByEmailAsync(employee.Email);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            return Ok();
        }

        private async Task UploadEmployeeImage(Employee employee)
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file != null)
            {
                var folderPath = $"{_appEnvironment.WebRootPath}/images/employees";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var employeeName = file.FileName.Replace(' ', '_');
                var savePath = $"{folderPath}/{employeeName}";
                using (var fileStream = new FileStream(savePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                employee.Photo = $"/images/employees/{employeeName}";
            }
        }
    }
}
