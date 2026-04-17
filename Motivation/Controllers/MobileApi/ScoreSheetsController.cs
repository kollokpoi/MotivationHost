using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motivation.Core.Interfaces;
using Motivation.Core.Services;
using Motivation.Data;
using Motivation.Models;
using Newtonsoft.Json;

namespace Motivation.Controllers.MobileApi
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ScoreSheetsController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ScoreSheetsController> _logger;

        private readonly IRepository<Rank> _ranksRepository;
        private readonly IRepository<Shift> _shiftsRepository;
        private readonly IRepository<ScoreSheet> _scoreSheetsRepository;
        private readonly IRepository<Qualification> _qualificationRepository;
        private readonly IRepository<EmployeePenalty> _employeePenaltiesRepository;
        private readonly IRepository<EmployeeTask> _employeeTasksRepository;
        private readonly IRepository<Penalty> _penaltiesRepository;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly ISalaryCalculator _salaryCalculator;
        private readonly IEfficiencyCalculator _efficiencyCalculator;
        private readonly RankCalculator _rankCalculator;
        private readonly IScoreSheetGenerator _scoreSheetGenerator;

        public ScoreSheetsController(
            ILogger<ScoreSheetsController> logger,
            UserManager<IdentityUser> userManager,
            IRepository<Rank> ranksRepository,
            IRepository<Shift> shiftsRepository,
            IEmployeesRepository employeesRepository,
            IRepository<ScoreSheet> scoreSheetsRepository,
            ISalaryCalculator salaryCalculator,
            IEfficiencyCalculator efficiencyCalculator,
            IRepository<EmployeePenalty> employeePenaltiesRepository,
            IRepository<EmployeeTask> employeeTasksRepository,
            IRepository<Penalty> penaltiesRepository,
            IRepository<Qualification> qualificationRepository,
            IScoreSheetGenerator scoreSheetGenerator
        )
        {
            _logger = logger;
            _ranksRepository = ranksRepository;
            _shiftsRepository = shiftsRepository;
            _employeesRepository = employeesRepository;
            _scoreSheetsRepository = scoreSheetsRepository;
            _salaryCalculator = salaryCalculator;
            _efficiencyCalculator = efficiencyCalculator;
            _penaltiesRepository = penaltiesRepository;
            _qualificationRepository = qualificationRepository;
            _employeePenaltiesRepository = employeePenaltiesRepository;
            _employeeTasksRepository = employeeTasksRepository;
            _userManager = userManager;
            _rankCalculator = new RankCalculator();
            _scoreSheetGenerator = scoreSheetGenerator;
        }

        [HttpGet]
        public async Task Get(int employeeId, int year, int month)
        {
            try
            {
                // var employee =
                //     await _employeesRepository.Entries.FirstOrDefaultAsync(e => e.Id == employeeId)
                //     ?? throw new Exception("Cannot find employee with this Id");
                var employee = await GetEmployee() ?? throw new Exception("Invalid User");

                // if (
                //     (user.Id != employee.Id)
                //     && !(employee.DepartmentId == user.DepartmentId && user.IsManager)
                // )
                // {
                //     Response.StatusCode = StatusCodes.Status403Forbidden;
                //     return;
                // }

                var scoreSheet = await _scoreSheetsRepository
                    .Entries.Include(s => s.CalculatedRank)
                    .Include(s => s.Rank)
                    .Include(s => s.NewRank)
                    .Where(s =>
                        s.EmployeeId == employee.Id
                        && s.StartPeriod.ToLocalTime().Month == month
                        && s.StartPeriod.ToLocalTime().Year == year
                    )
                    .FirstOrDefaultAsync();

                if (scoreSheet == null)
                {
                    Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                var penalties = _employeePenaltiesRepository.Entries.Where(p =>
                    p.EmployeeId == employee.Id
                    && p.Created.Year == year
                    && p.Created.Month == month
                );
                var penaltiesCount = await penalties.CountAsync();
                var penaltyPoints = await penalties.SumAsync(p => p.Penalty.Points);

                var json = JsonConvert.SerializeObject(
                    new
                    {
                        id = scoreSheet.Id,
                        is_confirmed = scoreSheet.IsSigned,
                        show_buttons = scoreSheet.IsSigned is null,
                        efficiency = scoreSheet.Efficiency,
                        score = scoreSheet.Score,
                        month = new
                        {
                            begginning = scoreSheet.StartPeriod.ToLocalTime().ToString("d"),
                            end = scoreSheet.EndPeriod.ToLocalTime().ToString("d"),
                        },
                        rank = new
                        {
                            last_rank = scoreSheet.Rank?.Number,
                            new_rank = scoreSheet.NewRank?.Number,
                            calculated_rank = scoreSheet.CalculatedRank?.Number,
                        },
                        qualification = employee?.Qualification?.Name,
                        shifts = new
                        {
                            shifts_count = scoreSheet.ShiftsCount,
                            working_time = $"{scoreSheet.WorkingTime} ч",
                        },
                        salary = scoreSheet.Salary,
                        penalties = new
                        {
                            penalties_count = penaltiesCount,
                            total_penalty_points = penaltyPoints,
                            penalties = penalties
                                .Select(p => new { p.Penalty.Description, p.Penalty.Points })
                                .ToArray(),
                        },
                    }
                );
                await Response.WriteAsync(json);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while getting scoresheet:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpGet("{scoreSheetId}/Excel")]
        public async Task<IActionResult> DownloadScoreSheetExcel(int scoreSheetId)
        {
            try
            {
                var employee = await GetEmployee() ?? throw new Exception("Invalid User");
                var scoreSheet = await _scoreSheetsRepository
                    .Entries.Include(s => s.Rank)
                    .Include(s => s.NewRank)
                    .Include(s => s.Employee)
                    .Include(s => s.CalculatedRank)
                    .Include(s => s.Qualification)
                    .FirstOrDefaultAsync(s => s.Id == scoreSheetId);

                var penalties = await _employeePenaltiesRepository
                    .Entries.Where(p =>
                        p.EmployeeId == employee.Id
                        && p.Created.Year == scoreSheet!.EndPeriod.Year
                        && p.Created.Month == scoreSheet.EndPeriod.Month
                    )
                    .Include(ep => ep.Penalty)
                    .Include(ep => ep.Employee)
                    .ToArrayAsync();

                if (scoreSheet is null)
                {
                    Response.StatusCode = StatusCodes.Status400BadRequest;
                    var res = new
                    {
                        error = new
                        {
                            message = "Score sheet with provided id doesn't exist",
                            type = "ERR_NOT_EXIST",
                        },
                    };
                    return StatusCode(400, res);
                }

                string tempPath = Path.GetTempPath();
                string tempFileName = Path.GetRandomFileName();
                string filePath = Path.Combine(tempPath, tempFileName);
                using var fs = new FileStream(filePath, FileMode.Create);
                await _scoreSheetGenerator.GenerateExcelForEmployee(
                    fs,
                    scoreSheet,
                    penalties.AsEnumerable()
                );
                var file = await System.IO.File.ReadAllBytesAsync(filePath);

                string fileName = "Оценочный лист.xlsx";
                string contentType =
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(file, contentType, fileName);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying to download score sheet:\n {e}";
                _logger.LogError(exceptionString);
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                return StatusCode(500, exceptionString);
            }
        }

        [HttpGet("{scoreSheetId}/PDF")]
        public async Task<IActionResult> DownloadScoreSheetPDF(int scoreSheetId)
        {
            try
            {
                var employee = await GetEmployee() ?? throw new Exception("Invalid User");
                var scoreSheet = await _scoreSheetsRepository
                    .Entries.Include(s => s.Rank)
                    .Include(s => s.NewRank)
                    .Include(s => s.Employee)
                    .Include(s => s.CalculatedRank)
                    .Include(s => s.Qualification)
                    .FirstOrDefaultAsync(s => s.Id == scoreSheetId);

                var penalties = await _employeePenaltiesRepository
                    .Entries.Where(p =>
                        p.EmployeeId == employee.Id
                        && p.Created.Year == scoreSheet!.EndPeriod.Year
                        && p.Created.Month == scoreSheet.EndPeriod.Month
                    )
                    .Include(ep => ep.Penalty)
                    .Include(ep => ep.Employee)
                    .ToArrayAsync();

                if (scoreSheet is null)
                {
                    Response.StatusCode = StatusCodes.Status400BadRequest;
                    var res = new
                    {
                        error = new
                        {
                            message = "Score sheet with provided id doesn't exist",
                            type = "ERR_NOT_EXIST",
                        },
                    };
                    return StatusCode(400, res);
                }

                string tempPath = Path.GetTempPath();
                string tempFileName = Path.GetRandomFileName();
                string filePath = Path.Combine(tempPath, tempFileName);
                using var fs = new FileStream(filePath, FileMode.Create);
                await _scoreSheetGenerator.GeneratePdfForEmployee(
                    fs,
                    scoreSheet,
                    penalties.AsEnumerable()
                );
                var file = await System.IO.File.ReadAllBytesAsync(filePath);

                string fileName = "Оценочный лист.pdf";
                string contentType = "application/pdf";

                return File(file, contentType, fileName);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying to download score sheet:\n {e}";
                _logger.LogError(exceptionString);
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                return StatusCode(500, exceptionString);
            }
        }

        [HttpGet("Excel")]
        public async Task<IActionResult> DownloadSummarySheetExcel(int year, int month)
        {
            try
            {
                var employees = await _employeesRepository
                    .Entries.Include(u => u.Position)
                    .Include(u => u.Qualification)
                    .Include(u => u.Rank)
                    .ToArrayAsync();

                var scoreSheets = new List<ScoreSheet>(employees.Count());
                foreach (var employee in employees)
                {
                    scoreSheets.Add(
                        await _scoreSheetGenerator.CreateScoreSheetForEmployee(
                            employee,
                            year,
                            month
                        )
                    );
                }

                string tempPath = Path.GetTempPath();
                string tempFileName = Path.GetRandomFileName();
                string filePath = Path.Combine(tempPath, tempFileName);
                using var fs = new FileStream(filePath, FileMode.Create);
                await _scoreSheetGenerator.CreateSummarySheet(fs, scoreSheets);
                var file = await System.IO.File.ReadAllBytesAsync(filePath);

                string fileName = "Сводный лист.xlsx";
                string contentType =
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(file, contentType, fileName);
            }
            catch (Exception e)
            {
                var exceptionString =
                    $"Error occured while trying to download summary sheet:\n {e}";
                _logger.LogError(exceptionString);
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                return StatusCode(500, exceptionString);
            }
        }

        // TODO: "scoresheetId" is actually an employee id, but for now i discard it
        // because i can get employee id from token
        [HttpPost("{scoresheetId}/Confirm")]
        public async Task ConfirmScoreSheet(int scoresheetId)
        {
            try
            {
                var employee = await GetEmployee() ?? throw new Exception("Invalid User");

                var scoreSheet =
                    await _scoreSheetsRepository
                        .Entries.OrderByDescending(ss => ss.StartPeriod)
                        .FirstOrDefaultAsync(s => s.EmployeeId == employee.Id)
                    ?? throw new Exception(
                        $"Scoresheet of an employee with id:{employee.Id} is not found"
                    );

                scoreSheet.IsSigned = true;
                employee.RankId = scoreSheet.NewRankId;

                await _employeesRepository.UpdateAsync(employee);
                await _scoreSheetsRepository.UpdateAsync(scoreSheet);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying confirm scoresheet:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        // TODO: "scoresheetId" is actually an employee id, but for now i discard it
        // because i can get employee id from token
        [HttpPost("{scoresheetId}/Decline")]
        public async Task DeclineScoreSheet(int scoresheetId)
        {
            try
            {
                var employee = await GetEmployee() ?? throw new Exception("Invalid User");
                var scoreSheet =
                    await _scoreSheetsRepository
                        .Entries.OrderByDescending(ss => ss.StartPeriod)
                        .FirstOrDefaultAsync(s => s.EmployeeId == employee.Id)
                    ?? throw new Exception(
                        $"Scoresheet an employee with id:{employee.Id} is not found"
                    );

                scoreSheet.IsSigned = false;
                await _scoreSheetsRepository.UpdateAsync(scoreSheet);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying decline scoresheet:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        private async Task<Employee?> GetEmployee()
        {
            var userEmail = User.Claims.FirstOrDefault(i => i.Type == ClaimTypes.Name);
            if (userEmail == null)
                return null;

            var user = await _userManager.FindByEmailAsync(userEmail.Value);
            if (user == null)
                return null;

            var employee = await _employeesRepository.Entries.FirstOrDefaultAsync(e =>
                e.UserId == user.Id
            );
            return employee;
        }
    }
}
