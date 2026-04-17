using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motivation.Data;
using Motivation.Models;
using Motivation.Data.Repositories;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Motivation.ViewModels;
using Newtonsoft.Json;

namespace Motivation.Controllers
{
    [Authorize(Roles = "Admins,Managers")]
    public class EmployeePenaltiesController : Controller
    {
        private readonly ILogger<EmployeePenaltiesController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IRepository<EmployeePenalty> _employeePenaltiesRepository;
        private readonly IRepository<Penalty> _penaltiesRepository;
        private readonly IEmployeesRepository _employeesRepository;

        public EmployeePenaltiesController(
            ILogger<EmployeePenaltiesController> logger,
            UserManager<IdentityUser> userManager,
            IRepository<EmployeePenalty> employeePenaltiesRepository,
            IRepository<Penalty> penaltiesRepository,
            IEmployeesRepository employeesRepository)
        {
            _logger = logger;
            _userManager = userManager;
            _employeePenaltiesRepository = employeePenaltiesRepository;
            _penaltiesRepository = penaltiesRepository;
            _employeesRepository = employeesRepository;
        }

        [HttpGet]
        [Authorize(Roles = "Admins,Managers")]
        public async Task<IActionResult> Index(int? year, int? month)
        {
            var now = DateTime.Now;
            int selectedYear = year ?? now.Year;
            int selectedMonth = month ?? now.Month;

            ViewBag.Year = selectedYear;
            ViewBag.Month = selectedMonth;

            var employeePenalties = await _employeePenaltiesRepository.Entries
                .Where(ep => ep.Created.Year == selectedYear && ep.Created.Month == selectedMonth)
                .OrderByDescending(ep => ep.Created)
                .ToListAsync();

            var employeePenaltiesViewModel = new EmployeePenaltiesViewModel
            {
                EmployeePenalties = employeePenalties.Select(ep => new EmployeePenaltyViewModel { EmployeePenalty = ep }).ToList()
            };

            return View(employeePenaltiesViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var employees = await _employeesRepository.Entries.ToListAsync();
            var penalties = await _penaltiesRepository.Entries.ToListAsync();
            var addEmployeePenaltyViewModel = new AddEmployeePenaltyViewModel
            {
                Employees = employees,
                Penalties = penalties,
            };
            return View(addEmployeePenaltyViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Add(EmployeePenalty employeePenalty)
        {
            if (employeePenalty == null)
                return BadRequest($"EmployeePenalty is null");


            var userEmail = User.Claims.FirstOrDefault(i => i.Type == ClaimTypes.Name);
            if (userEmail == null)
                return BadRequest($"userEmail is null");


            var user = await _userManager.FindByNameAsync(userEmail.Value);
            if (user == null)
                return BadRequest($"user is null");


            var author = await _employeesRepository.Entries.FirstOrDefaultAsync(e =>
                e.Email == user.Email
            );
            if (author == null)
                return BadRequest($"author is null");


            employeePenalty.AuthorId = author.Id;
            employeePenalty.Created = DateTime.UtcNow;

            await _employeePenaltiesRepository.CreateAsync(employeePenalty);
            return Redirect("/EmployeePenalties");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var employeePenalty = await _employeePenaltiesRepository
                .Entries.Where(ep => ep.Id == id)
                .FirstOrDefaultAsync();

            if (employeePenalty == null)
                return StatusCode(500);

            var employees = await _employeesRepository.Entries.ToListAsync();
            var penalties = await _penaltiesRepository.Entries.ToListAsync();

            var editEmployeePenaltyViewModel = new EditEmployeePenaltyViewModel
            {
                EmployeePenalty = employeePenalty,
                Employees = employees,
                Penalties = penalties
            };
            return View(editEmployeePenaltyViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EmployeePenalty employeePenalty)
        {
            if (employeePenalty == null)
                return BadRequest("Employee Penalty is null");

            var existingPenalty = await _employeePenaltiesRepository.Entries
                .FirstOrDefaultAsync(ep => ep.Id == employeePenalty.Id);

            if (existingPenalty == null)
                return NotFound("Employee Penalty not found");

            existingPenalty.PenaltyId = employeePenalty.PenaltyId;
            existingPenalty.EmployeeId = employeePenalty.EmployeeId;
            existingPenalty.Explanation = employeePenalty.Explanation;
            existingPenalty.Updated = DateTime.UtcNow;

            await _employeePenaltiesRepository.UpdateAsync(existingPenalty);

            return Redirect("/EmployeePenalties");
        }

        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            var data = await Request.ReadFromJsonAsync<EmployeePenalty>();
            var id = data?.Id ?? -1;
            if (id <= 0)
                return StatusCode(500);

            // var employeePenalty = await _employeePenaltiesRepository.Entries.FirstOrDefaultAsync(ep => ep.Id == id);
            // if (employeePenalty == null)
            //     return StatusCode(500);
            await _employeePenaltiesRepository.DeleteAsync(id);
            return Ok();
            // return Redirect("/EmployeePenalties");
        }
    }
}
