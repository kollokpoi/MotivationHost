using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motivation.Core;
using Motivation.Data;
using Motivation.Models;
using Motivation.ViewModels;

namespace Motivation.Controllers
{
    [Authorize(Roles = "Admins, Managers")]
    public class ShiftsController : Controller
    {
        private const int MaxHours = 8;

        private readonly ILogger<ShiftsController> _logger;
        private readonly IWebHostEnvironment _appEnvironment;

        private readonly IRepository<Shift> _shiftsRepository;
        private readonly IEmployeesRepository _employeesRepository;

        public ShiftsController(
            ILogger<ShiftsController> logger,
            IRepository<Shift> shiftsRepository,
            IEmployeesRepository employeesRepository,
            IWebHostEnvironment appEnvironment
        )
        {
            _logger = logger;
            _shiftsRepository = shiftsRepository;
            _employeesRepository = employeesRepository;
            _appEnvironment = appEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int year, int month)
        {
            var periodStartDate = new DateTime(year, month, 1);
            if (periodStartDate > DateTime.Now)
                return StatusCode(StatusCodes.Status400BadRequest);

            var shiftsViewModel = await GetShiftsViewModel(year, month);
            return View(shiftsViewModel);
        }

        private async Task<ShiftsViewModel> GetShiftsViewModel(int year, int month)
        {
            var periodStartDate = new DateTime(year, month, 1);
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var shiftViewModels = new List<ShiftViewModel>();
            foreach (var employee in await _employeesRepository.Entries.ToListAsync())
            {
                var shifts = await _shiftsRepository
                    .Entries.Where(s => s.EmployeeId == employee.Id)
                    .Where(s => s.Started.Year == year && s.Started.Month == month)
                    .ToListAsync();

                var workingHours = new int[daysInMonth];
                var comments = new string[daysInMonth];

                foreach (var shift in shifts)
                {
                    if (shift.Ended == DateTime.MinValue)
                        continue;

                    var dayIndex = shift.Started.Day - 1;
                    var workMinutes = (int)
                        Math.Floor((shift.Ended - shift.Started - shift.PauseTime).TotalMinutes);
                    var workHours = workMinutes / 60 + (workMinutes % 60 >= 45 ? 1 : 0);
                    workingHours[dayIndex] = Math.Min(
                        Math.Min(
                            workHours,
                            (int)(shift.LegalEndTime - shift.LegalStartTime).TotalHours
                        ),
                        MaxHours
                    );

                    var employeeStartTime = shift.LegalStartTime.ToLocalTime();
                    var employeeEndTime = shift.LegalEndTime.ToLocalTime();
                    var shiftStart = shift.Started.ToLocalTime();
                    var shiftEnd = shift.Ended.ToLocalTime();
                    var shiftStartDay = new DateTime(
                        shiftStart.Year,
                        shiftStart.Month,
                        shiftStart.Day
                    );

                    var comment = new StringBuilder();
                    comment.Append($"Начало: {shiftStart:g}<br>Конец: {shiftEnd:g}");

                    void AddCommentPart(TimeSpan time, string name)
                    {
                        if (time <= TimeSpan.Zero)
                            return;
                        comment.Append($"<br>{name}: ");
                        if (time.Hours > 0)
                            comment.Append($"{time.Hours:0} ч ");
                        comment.Append($"{time.Minutes:0} м ");
                        comment.Append($"{time.Seconds:0} с");
                    }

                    var lateness =
                        shiftStart - employeeStartTime - new TimeSpan(shiftStartDay.Ticks);
                    var earlyFinish =
                        employeeEndTime - (shiftEnd - new TimeSpan(shiftStartDay.Ticks));

                    AddCommentPart(shift.PauseTime, "Перерыв");
                    AddCommentPart(lateness, "Опоздание");
                    AddCommentPart(earlyFinish, "Уход раньше");

                    comments[dayIndex] = comment.ToString();
                }

                var shiftViewModel = new ShiftViewModel
                {
                    Employee = employee,
                    WorkingHours = workingHours.ToList(),
                    Comments = comments.ToList(),
                };

                shiftViewModels.Add(shiftViewModel);
            }

            var shiftsViewModel = new ShiftsViewModel
            {
                EmployeesShifts = shiftViewModels,
                StartDate = periodStartDate,
                EndDate = periodStartDate.AddDays(daysInMonth - 1),
            };

            return shiftsViewModel;
        }

        [HttpGet]
        public async Task<IActionResult> Export(int year, int month)
        {
            var periodStartDate = new DateTime(year, month, 1);
            if (periodStartDate > DateTime.Now)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            var shiftsViewModel = await GetShiftsViewModel(year, month);

            var folderPath = $"{_appEnvironment.ContentRootPath}/Files";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var path = $"{folderPath}/WorkingTable.xlsx";

            var tableGenerator = new WorkingTimeTableGenerator();
            tableGenerator.Generate(path, shiftsViewModel);
            string type = "application/xlsx";
            string name = $"Табель {shiftsViewModel.StartDate:d}-{shiftsViewModel.EndDate:d}.xlsx";
            var fs = new FileStream(path, FileMode.Open);
            return File(fs, type, name);
        }
    }
}
