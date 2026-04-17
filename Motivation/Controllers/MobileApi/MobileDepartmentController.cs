using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motivation.Core.Interfaces;
using Motivation.Data;
using Motivation.Models;
using Newtonsoft.Json;

namespace Motivation.Controllers.MobileApi
{
    [Route("api/v{version:apiVersion}/Departments")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MobileDepartmentController : Controller
    {
        private readonly ILogger<MobileDepartmentController> _logger;
        private readonly IRepository<Department> _departmentsRepository;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly IRepository<EmployeePenalty> _employeePenaltiesRepository;
        private readonly ISalaryCalculator _salaryCalculator;
        private readonly IEfficiencyCalculator _efficiencyCalculator;
        private readonly IDepartmentGetter _departmentGetter;

        public MobileDepartmentController(ILogger<MobileDepartmentController> logger,
            IRepository<Department> departmentsRepository,
            IEmployeesRepository employeesRepository,
            IRepository<EmployeePenalty> employeePenaltiesRepository,
            ISalaryCalculator salaryCalculator,
            IEfficiencyCalculator efficiencyCalculator,
            IDepartmentGetter departmentGetter)
        {
            _logger = logger;
            _departmentsRepository = departmentsRepository;
            _employeesRepository = employeesRepository;
            _employeePenaltiesRepository = employeePenaltiesRepository;
            _salaryCalculator = salaryCalculator;
            _efficiencyCalculator = efficiencyCalculator;
            _departmentGetter = departmentGetter;
        }

        [HttpGet("{id}")]
        public async Task GetDepartment(int id)
        {
            try
            {
                var department = await _departmentsRepository.Entries.Where(d => d.Id == id).FirstOrDefaultAsync() ?? throw new Exception($"Нет такого подразделения id:{id}!");

                var childrenDepartments = await _departmentsRepository.Entries.Where(d => d.ParentId == department.Id && d.Id != d.ParentId).ToListAsync();
                var depObj = await _departmentGetter.GetDepartmentObjectAsync(department.Id);
                object? underDepObjs = null;
                if (childrenDepartments.Any())
                {
                    underDepObjs = await Task.WhenAll(childrenDepartments.Select(d => _departmentGetter.GetDepartmentObjectForEmployeeAsync(d.Id)));
                }

                var response = new
                {
                    department = depObj,
                    under_departments = underDepObjs
                };

                var json = JsonConvert.SerializeObject(response);
                await Response.WriteAsync(json);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying get department:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpGet("{id}/Employees")]
        public async Task Employees(int id)
        {
            try
            {
                var department = await _departmentsRepository.Entries.Where(d => d.Id == id).FirstOrDefaultAsync();
                var employees = await _employeesRepository.Entries.Where(e => e.DepartmentId == id).ToListAsync();

                var manager = employees.FirstOrDefault(e => e.IsManager);
                if (manager != null)
                {
                    employees.Remove(manager);
                }

                var workersList = employees.Select(e => new
                {
                    id = e.Id,
                    full_name = e.GetFullName(),
                    position = e.Position?.Name,
                    email = e.Email,
                    photo = e.Photo,
                    rank = e.Rank?.Number
                }).ToList();

                var json = JsonConvert.SerializeObject(new
                {
                    id = department?.Id,
                    name = department?.Name,
                    manager = new
                    {
                        id = manager?.Id,
                        full_name = manager?.GetFullName(),
                        position = manager?.Position?.Name,
                        email = manager?.Email,
                        photo = manager?.Photo,
                        rank = manager?.Rank?.Number
                    },
                    workers = workersList
                });

                await Response.WriteAsync(json);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying get employees from department:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpGet("{id}/Penalties")]
        public async Task GetDepartmentPenaltiesForMonth(int id)
        {
            try
            {
                var month = DateTime.Now.Month;
                var year = DateTime.Now.Year;
                var startPeriod = new DateTime(year, month, 1);
                var endPeriod = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59);

                var departmentEmployeesIds = await _employeesRepository.Entries.Where(e => e.DepartmentId == id).Select(e => e.Id).ToListAsync();
                var penalties = _employeePenaltiesRepository.Entries.Where(p => departmentEmployeesIds.Contains(p.EmployeeId) && p.Created.Year == year && p.Created.Month == month).OrderByDescending(t => t.Id);
                var responsePenalties = await penalties.Select(p => new
                {
                    p.Penalty.Description,
                    p.Explanation,
                    Created = p.Created.ToLocalTime(),
                    BadEmployee = new
                    {
                        Id = p.Employee.Id,
                        FullName = p.Employee.GetShortName(),
                        Email = p.Employee.Email,
                        Position = p.Employee.Position.Name,
                        Rank = p.Employee.Rank.Number,
                        Photo = p.Employee.Photo
                    },
                    Author = new
                    {
                        Id = p.Author.Id,
                        FullName = p.Author.GetShortName(),
                        Email = p.Author.Email,
                        Position = p.Author.Position.Name,
                        Rank = p.Author.Rank.Number,
                        Photo = p.Author.Photo
                    }
                }).ToArrayAsync();

                var json = JsonConvert.SerializeObject(new
                {
                    time_interval = $"{startPeriod:d} - {endPeriod:d}",
                    penalties = responsePenalties
                });
                await Response.WriteAsync(json);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying getting employee's penalties:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }
    }
}
