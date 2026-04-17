using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motivation.Core.Interfaces;
using Motivation.Core.Services;
using Motivation.Data;
using Motivation.Models;
using Motivation.ViewModels;

namespace Motivation.Controllers
{
    [Authorize(Roles = "Admins, Managers")]
    public class SummarySheetController : Controller
    {
        private readonly ILogger<SummarySheetController> _logger;
        private readonly IWebHostEnvironment _appEnvironment;

        private readonly IEmployeesRepository _employeesRepository;
        private readonly IScoreSheetGenerator _scoreSheetGenerator;

        public SummarySheetController(
            ILogger<SummarySheetController> logger,
            IEmployeesRepository employeesRepository,
            IScoreSheetGenerator scoreSheetGenerator,
            IWebHostEnvironment appEnvironment
        )
        {
            _logger = logger;
            _employeesRepository = employeesRepository;
            _appEnvironment = appEnvironment;
            _scoreSheetGenerator = scoreSheetGenerator;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int year, int month)
        {
            var periodStartDate = new DateTime(year, month, 1);
            if (periodStartDate > DateTime.Now)
                return StatusCode(StatusCodes.Status400BadRequest);

            var shiftsViewModel = await GetSummarySheetViewModel(year, month);
            return View(shiftsViewModel);
        }

        private async Task<SummarySheetViewModel> GetSummarySheetViewModel(int year, int month)
        {
            var periodStartDate = new DateTime(year, month, 1);
            var daysInMonth = DateTime.DaysInMonth(year, month);

            var employees = await _employeesRepository
                .Entries.Include(u => u.Position)
                .Include(u => u.Qualification)
                .Include(u => u.Rank)
                .ToArrayAsync();

            var scoreSheets = new List<ScoreSheet>(employees.Count());
            foreach (var employee in employees)
            {
                scoreSheets.Add(
                    await _scoreSheetGenerator.CreateScoreSheetForEmployee(employee, year, month)
                );
            }

            var scoreSheetsViewModel = new SummarySheetViewModel
            {
                ScoreSheets = scoreSheets,
                StartDate = periodStartDate,
                EndDate = periodStartDate.AddDays(daysInMonth - 1),
                BasePoints = RankCalculator.BasePoints,
            };

            return scoreSheetsViewModel;
        }

        [HttpGet]
        public async Task<IActionResult> Export(int year, int month)
        {
            var periodStartDate = new DateTime(year, month, 1);
            if (periodStartDate > DateTime.Now)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            var summarySheetViewModel = await GetSummarySheetViewModel(year, month);

            string tempPath = Path.GetTempPath();
            string tempFileName = Path.GetRandomFileName();
            string filePath = Path.Combine(tempPath, tempFileName);
            using var fs = new FileStream(filePath, FileMode.Create);
            await _scoreSheetGenerator.CreateSummarySheet(fs, summarySheetViewModel.ScoreSheets);
            var file = await System.IO.File.ReadAllBytesAsync(filePath);

            string name =
                $"Сводный лист {summarySheetViewModel.StartDate:d}-{summarySheetViewModel.EndDate:d}.xlsx";
            string contentType =
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(file, contentType, name);
        }
    }
}
