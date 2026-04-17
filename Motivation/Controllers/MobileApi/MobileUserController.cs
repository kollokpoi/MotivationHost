using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motivation.Core.Interfaces;
using Motivation.Core.Services;
using Motivation.Data;
using Motivation.Data.Repositories;
using Motivation.Models;
using Motivation.Models.Mobile;
using Motivation.Options;
using Newtonsoft.Json;

namespace Motivation.Controllers.MobileApi
{
    [Route("api/v{version:apiVersion}/Employee")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MobileUserController : ControllerBase
    {
        private readonly ILogger<MobileUserController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IRepository<Shift> _shiftsRepository;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly IRepository<EmployeePenalty> _employeePenaltiesRepository;
        private readonly IRepository<EmployeeTask> _employeeTasksRepository;
        private readonly ISalaryCalculator _salaryCalculator;
        private readonly IEfficiencyCalculator _efficiencyCalculator;
        private readonly IDepartmentGetter _departmentGetter;
        private readonly BitrixTasksRepository _bitrixTasksRepository;
        private readonly RankCalculator _rankCalculator;
        private readonly IConfiguration _configuration;
        private readonly BitrixTimemanRepository _bitrixTimemanRepository;

        private readonly HttpClient _httpClient;

        private readonly string _origin;

        public MobileUserController(
            ILogger<MobileUserController> logger,
            IRepository<Shift> shiftsRepository,
            IEmployeesRepository employeesRepository,
            IRepository<EmployeePenalty> employeePenaltiesRepository,
            IRepository<EmployeeTask> employeeTasksRepository,
            UserManager<IdentityUser> userManager,
            ISalaryCalculator salaryCalculator,
            IEfficiencyCalculator efficiencyCalculator,
            IDepartmentGetter departmentGetter,
            IConfiguration configuration,
            BitrixTasksRepository bitrixTasksRepository,
            BitrixTimemanRepository bitrixTimemanRepository
        )
        {
            _logger = logger;
            _shiftsRepository = shiftsRepository;
            _employeesRepository = employeesRepository;
            _employeePenaltiesRepository = employeePenaltiesRepository;
            _employeeTasksRepository = employeeTasksRepository;
            _userManager = userManager;
            _salaryCalculator = salaryCalculator;
            _efficiencyCalculator = efficiencyCalculator;
            _departmentGetter = departmentGetter;
            _bitrixTasksRepository = bitrixTasksRepository;
            _rankCalculator = new RankCalculator();
            _configuration = configuration;
            _bitrixTimemanRepository = bitrixTimemanRepository;

            var opts = _configuration.Get<BitrixOptions>();
            _origin = opts!.BitrixPortalHost;
            _httpClient = new HttpClient { BaseAddress = new Uri(opts.BitrixBridgeAppURL) };
        }

        [HttpGet("{id}")]
        public async Task Get(int id)
        {
            try
            {
                var month = DateTime.Now.Month;
                var year = DateTime.Now.Year;

                var previousMonth = DateTime.Now.AddMonths(-1).Month;
                var previousYear = DateTime.Now.AddMonths(-1).Year;

                var employee =
                    await _employeesRepository
                        .Entries.Where(e => e.Id == id)
                        .Include(e => e.Department)
                        .FirstOrDefaultAsync() ?? throw new Exception("Нет такого сотрудника");

                var salary = await _salaryCalculator.CalculateSalaryForCurrentMonth(employee);
                var budgetCard = new { ammount = salary.ToString("C") };

                var employeePenaltyPoints = await _employeePenaltiesRepository
                    .Entries.Where(p =>
                        p.EmployeeId == id && p.Created.Year == year && p.Created.Month == month
                    )
                    .SumAsync(p => p.Penalty.Points);
                var qualificationPoints = employee.Qualification?.Points;
                var score = _rankCalculator.CalculateScore(
                    employeePenaltyPoints,
                    qualificationPoints ?? 0
                );
                var previousRank = employee.Rank.Number;
                var calculatedRank = _rankCalculator.CalculateRankNumber(score);
                var newRank = _rankCalculator.CalculateNewRankWithLimits(
                    previousRank,
                    calculatedRank
                );

                var nextRankCard = new Card
                {
                    Name = "nextRank",
                    Title = "Следующий Ранг",
                    Content = newRank.ToString(),
                    SortIndex = 1,
                };

                var efficiency =
                    await _efficiencyCalculator.CalculateForEmployeeForCurrentMonthAsync(id);
                var efficiencyCard = new Card
                {
                    Name = "efficiency",
                    Title = "Эффективность",
                    Content = efficiency.ToString(),
                    SortIndex = 2,
                };

                var penaltiesCount = await _employeePenaltiesRepository
                    .Entries.Where(p =>
                        p.EmployeeId == id && p.Created.Year == year && p.Created.Month == month
                    )
                    .CountAsync();
                var previousPenaltiesCount = await _employeePenaltiesRepository
                    .Entries.Where(p =>
                        p.EmployeeId == id
                        && p.Created.Year == previousYear
                        && p.Created.Month == previousMonth
                    )
                    .CountAsync();
                var penaltiesProgress = -penaltiesCount * 100;
                if (previousPenaltiesCount > 0)
                {
                    penaltiesProgress =
                        (previousPenaltiesCount - penaltiesCount) / previousPenaltiesCount;
                }

                var penaltiesCard = new Card
                {
                    Name = "penalties",
                    Title = "Замечания",
                    Content = penaltiesCount.ToString(),
                    SortIndex = 3,
                    Clickable = penaltiesCount > 0,
                };

                var penaltiesProgressionCard = new Card
                {
                    Name = "penaltiesProgression",
                    Title = "Прогресс по замечаниям",
                    Content = penaltiesProgress.ToString(),
                    SortIndex = 4,
                };

                var workingTime = await _shiftsRepository
                    .Entries.Where(s => s.EmployeeId == employee.Id)
                    .Where(s => s.Started.Year == year && s.Started.Month == month)
                    .Where(s => s.Ended != DateTime.MinValue)
                    .Select(s =>
                        Math.Min(
                            (int)Math.Floor((s.Ended - s.Started - s.PauseTime).TotalHours),
                            (int)(s.LegalEndTime - s.LegalStartTime).TotalHours
                        )
                    )
                    .SumAsync();

                var workingTimeCard = new Card
                {
                    Name = "workingTime",
                    Title = "Отработано часов",
                    Content = workingTime.ToString(),
                    SortIndex = 5,
                };

                var tasks = _employeeTasksRepository.Entries.Where(t => t.EmployeeId == id);
                var tasksCount = await tasks.CountAsync();
                var tasksInProgressCount = await tasks.CountAsync(t =>
                    t.Status == Models.TaskStatus.InProgress
                );
                var tasksNewCount = await tasks.CountAsync(t => t.Status == Models.TaskStatus.New);

                var cards = new List<object>
                {
                    nextRankCard,
                    efficiencyCard,
                    penaltiesCard,
                    penaltiesProgressionCard,
                    workingTimeCard,
                };

                var buttons = new List<string>();

                var user = await GetCaller() ?? throw new Exception("User cannot be null");

                await CheckCanEmployeeStartWorkingDay(user);

                if (id == user.Id)
                {
                    if (user.Status != EmployeeStatus.WorkComplete)
                        buttons.Add("start_work");
                    buttons.Add("get_scoresheet");
                }

                if (
                    (
                        employee.DepartmentId == user.DepartmentId
                        && user.IsManager
                        && employee.Id != user.Id
                    )
                    || (
                        employee.IsManager
                        && user.IsManager
                        && employee.Department?.ParentId == user.DepartmentId
                    )
                )
                {
                    buttons.Add("add_penalty");
                    buttons.Add("create_task");
                    if (!buttons.Contains("get_scoresheet"))
                        buttons.Add("get_scoresheet");

                    var tasksCard = new Card
                    {
                        Name = "tasks",
                        Title = "Задачи",
                        Content =
                            $"{tasksNewCount}/{tasksInProgressCount}/{tasksCount - tasksNewCount - tasksInProgressCount}",
                        SortIndex = 6,
                        Clickable = true,
                    };

                    cards.Add(tasksCard);
                }

                var maxWorkingTime = employee.EndTime - employee.StartTime;
                var workTime = TimeSpan.Zero;
                if (employee.Status != EmployeeStatus.Abscent)
                {
                    var currentShift = await _shiftsRepository
                        .Entries.Where(s => s.EmployeeId == employee.Id)
                        .OrderBy(s => s.Id)
                        .LastOrDefaultAsync();
                    if (currentShift != null)
                    {
                        workTime = DateTime.UtcNow - currentShift.Started - currentShift.PauseTime;
                        if (employee.Status == EmployeeStatus.AtBreak)
                        {
                            workTime -= DateTime.UtcNow - currentShift.LastPauseStart;
                        }
                    }
                }

                var department =
                    user.IsManager && employee.Id == user.Id
                        ? await _departmentGetter.GetDepartmentObjectForEmployeeAsync(
                            employee.DepartmentId
                        )
                        : null;

                var employeeResponse = new
                {
                    user = new
                    {
                        id = employee.Id,
                        full_name = $"{employee.LastName} {employee.FirstName} {employee.MiddleName}",
                        position = employee.Position?.Name,
                        position_id = employee.Position?.Id,
                        email = employee.Email,
                        photo = employee.Photo,
                        rank = employee.Rank?.Number,
                    },
                    tasks_count = tasksCount,
                    work_time = new
                    {
                        status = employee.Status,
                        time = new
                        {
                            hours = workTime.Hours,
                            minutes = workTime.Minutes,
                            seconds = workTime.Seconds,
                        },
                        max_time = new
                        {
                            hours = maxWorkingTime.Hours,
                            minutes = maxWorkingTime.Minutes,
                            seconds = maxWorkingTime.Seconds,
                        },
                    },
                    buttons = buttons,
                    budget = budgetCard,
                    information = cards,
                    departments = department == null ? new object[] { } : [department],
                };

                var json = JsonConvert.SerializeObject(employeeResponse);
                await Response.WriteAsync(json);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying get employee:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpGet("{id}/Penalties")]
        public async Task GetEmployeePenaltiesForMonth(int id)
        {
            try
            {
                var month = DateTime.Now.Month;
                var year = DateTime.Now.Year;
                var startPeriod = new DateTime(year, month, 1);
                var endPeriod = new DateTime(
                    year,
                    month,
                    DateTime.DaysInMonth(year, month),
                    23,
                    59,
                    59
                );

                var penalties = _employeePenaltiesRepository
                    .Entries.Where(p =>
                        p.EmployeeId == id && p.Created.Year == year && p.Created.Month == month
                    )
                    .OrderByDescending(t => t.Id);
                var responsePenalties = await penalties
                    .Select(p => new
                    {
                        p.Penalty.Description,
                        p.Explanation,
                        Created = p.Created.ToLocalTime().ToString("hh:mm dd.MM.yyyy"),
                        Author = new
                        {
                            Id = p.Author.Id,
                            FullName = p.Author.GetShortName(),
                            Email = p.Author.Email,
                            Position = p.Author.Position.Name,
                            Rank = p.Author.Rank.Number,
                            Photo = p.Author.Photo,
                        },
                    })
                    .ToArrayAsync();

                var json = JsonConvert.SerializeObject(
                    new
                    {
                        time_interval = $"{startPeriod:d} - {endPeriod:d}",
                        penalties = responsePenalties,
                    }
                );
                await Response.WriteAsync(json);
            }
            catch (Exception e)
            {
                var exceptionString =
                    $"Error occured while trying getting employee's penalties:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpPost("{employeeId}/Penalties")]
        [Authorize(Roles = "Managers")]
        public async Task AddPenaltyToEmployee(
            int employeeId,
            [FromBody] MobilePenaltyModel penalty
        )
        {
            try
            {
                var manager = await GetCaller() ?? throw new Exception("Manager cannot be null");
                var employeePenalty = new EmployeePenalty
                {
                    EmployeeId = employeeId,
                    PenaltyId = penalty.Id,
                    Explanation = penalty.Text,
                    AuthorId = manager.Id,
                };
                await _employeePenaltiesRepository.CreateAsync(employeePenalty);
            }
            catch (Exception e)
            {
                var exceptionString =
                    $"Error occured while trying getting employees penalties:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpGet("{id}/Tasks")]
        public async Task GetEmployeeTasks(
            int id,
            int page = 1,
            int pageSize = 0,
            Models.TaskStatus? status = null
        )
        {
            var user = await GetCaller() ?? throw new Exception("User cannot be null");
            try
            {
                var month = DateTime.Now.Month;
                var year = DateTime.Now.Year;
                var startPeriod = new DateTime(year, month, 1);
                var endPeriod = new DateTime(
                    year,
                    month,
                    DateTime.DaysInMonth(year, month),
                    23,
                    59,
                    59
                );

                var tasksQuery = _employeeTasksRepository.Entries.Where(t => t.EmployeeId == id);

                if (status is null)
                {
                    throw new Exception("status cannot be null");
                }

                var employee = await _employeesRepository.Entries.FirstOrDefaultAsync(u =>
                    u.Id == id
                );
                if (employee == null)
                {
                    throw new Exception("User is null");
                }

                tasksQuery = tasksQuery.OrderByDescending(t => t.Id);
                // Turn off for now, because the pagination is useless
                // .Skip(pageSize * (page - 1))
                // .Take(pageSize);


                var tasks = new List<MobileResponseTaskModel>();

                var responseTasks = await tasksQuery
                    .Select(t => new MobileResponseTaskModel
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        DeadLine =
                            t.Deadline != null
                                ? t.Deadline.Value.ToLocalTime().ToString("d")
                                : null,
                        Status = t.Status,
                        Author = new MobileResponseEmployeeModel
                        {
                            Id = t.Author!.Id,
                            FullName = t.Author.GetShortName(),
                            Email = t.Author.Email,
                            Photo = t.Author.Photo,
                        },
                    })
                    .ToListAsync();

                tasks.AddRange(responseTasks);

                if (employee.BitrixUserId != 0)
                {
                    // WARNING: This is a workaround to get the tasks from the Bitrix Bridge app
                    // We need to get the tasks from the Bitrix Bridge app because the tasks are stored in the Bitrix database
                    // and we don't have a way to get them from the Motivation database
                    try
                    {
                        var bitrixTasks = await _bitrixTasksRepository.GetTasksByEmployeeAndStatus(
                            employee,
                            status
                        );
                        var mobileTasks =
                            await _bitrixTasksRepository.TranslateBitrixTasksToMobileTask(
                                bitrixTasks
                            );
                        tasks.AddRange(mobileTasks);
                    }
                    catch (Exception e)
                    {
                        var exceptionString =
                            $"Error occured while trying get employees tasks from Bitrix:\n {e}";
                        _logger.LogError(exceptionString);
                    }
                }

                var json = JsonConvert.SerializeObject(
                    new
                    {
                        time_interval = $"{startPeriod:d} - {endPeriod:d}",
                        page = page,
                        pageLength = pageSize,
                        tasks = tasks,
                    }
                );
                await Response.WriteAsync(json);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying get employees tasks:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpPost("{id}/Tasks")]
        public async Task CreateEmployeeTask(int id, [FromBody] MobileTaskModel task)
        {
            try
            {
                var manager = await GetCaller() ?? throw new Exception("Manager cannot be null");
                var deadline = DateTime.MinValue;
                var deadlineParsed = DateTime.TryParseExact(
                    task.Deadline,
                    "dd.MM.yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out deadline
                );
                if (deadlineParsed)
                    deadline = deadline.ToUniversalTime();
                var employeeTask = new EmployeeTask
                {
                    EmployeeId = id,
                    AuthorId = manager.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Deadline = deadline,
                };

                await _employeeTasksRepository.CreateAsync(employeeTask);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying create employees task:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpGet("Work/Status")]
        public async Task GetWorkingDayStatus()
        {
            try
            {
                var employee = await GetCaller() ?? throw new Exception("User cannot be null");

                var status = "";
                if (
                    employee.Status == EmployeeStatus.Abscent
                    || employee.Status == EmployeeStatus.WorkComplete
                )
                {
                    status = "CLOSED";
                }
                else if (employee.Status == EmployeeStatus.AtBreak)
                {
                    status = "PAUSED";
                }
                else
                {
                    status = "OPENED";
                }

                var json = new { status = status };

                await Response.WriteAsJsonAsync(json);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying get employee status:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpPost("Work/Start")]
        public async Task<IActionResult> StartWorkingDay()
        {
            try
            {
                var employee = await GetCaller();
                if (employee == null)
                    return BadRequest();

                if (
                    employee.Status == EmployeeStatus.Abscent
                    || employee.Status == EmployeeStatus.WorkComplete
                )
                {
                    var shift = new Shift
                    {
                        EmployeeId = employee.Id,
                        Started = DateTime.UtcNow,
                        LegalStartTime = employee.StartTime,
                        LegalEndTime = employee.EndTime,
                    };

                    // 0 means a user hasn't connected to this app yet
                    if (employee.BitrixUserId != 0)
                    {
                        try
                        {
                            Console.WriteLine(
                                "bitrix user id: " + employee.BitrixUserId.ToString()
                            );
                            await _bitrixTimemanRepository.Do("open", employee.BitrixUserId);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(JsonConvert.SerializeObject(e));
                        }
                    }
                    await _shiftsRepository.CreateAsync(shift);
                }

                if (employee.Status == EmployeeStatus.AtBreak)
                {
                    var shift = await _shiftsRepository
                        .Entries.Where(s => s.EmployeeId == employee.Id)
                        .OrderBy(s => s.Id)
                        .LastOrDefaultAsync();
                    if (shift != null)
                    {
                        if (employee.BitrixUserId != 0)
                        {
                            try
                            {
                                await _bitrixTimemanRepository.Do("open", employee.BitrixUserId);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(JsonConvert.SerializeObject(e));
                            }
                        }
                        shift.PauseTime += DateTime.UtcNow - shift.LastPauseStart;
                        await _shiftsRepository.UpdateAsync(shift);
                    }
                }

                await _employeesRepository.UpdateEmployeeStatus(employee.Id, EmployeeStatus.AtWork);
                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(JsonConvert.SerializeObject(e));
                return StatusCode(500);
            }
        }

        [HttpPost("Work/Pause")]
        public async Task<IActionResult> PauseWorkingDay()
        {
            try
            {
                var employee = await GetCaller();
                if (employee == null)
                    return BadRequest();
                if (employee.Status != EmployeeStatus.AtWork)
                    return BadRequest();

                var shift = await _shiftsRepository
                    .Entries.Where(s => s.EmployeeId == employee.Id)
                    .OrderBy(s => s.Id)
                    .LastOrDefaultAsync();
                if (shift == null)
                    return BadRequest();

                if (employee.BitrixUserId != 0)
                {
                    try
                    {
                        await _bitrixTimemanRepository.Do("pause", employee.BitrixUserId);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(JsonConvert.SerializeObject(e));
                    }
                }
                shift.LastPauseStart = DateTime.UtcNow;
                await _shiftsRepository.UpdateAsync(shift);
                await _employeesRepository.UpdateEmployeeStatus(
                    employee.Id,
                    EmployeeStatus.AtBreak
                );

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(JsonConvert.SerializeObject(e));
                return StatusCode(500);
            }
        }

        [HttpPost("Work/End")]
        public async Task<IActionResult> EndWorkingDay()
        {
            try
            {
                var employee = await GetCaller();
                if (employee == null)
                    return BadRequest();
                if (
                    employee.Status != EmployeeStatus.AtWork
                    && employee.Status != EmployeeStatus.AtBreak
                )
                    return BadRequest("Cannot end of working day if not at work or at break");

                var shift = await _shiftsRepository
                    .Entries.Where(s => s.EmployeeId == employee.Id)
                    .OrderBy(s => s.Id)
                    .LastOrDefaultAsync();
                if (shift == null)
                    return Ok();

                if (employee.Status == EmployeeStatus.AtBreak)
                {
                    shift.PauseTime += DateTime.UtcNow - shift.LastPauseStart;
                }

                if (employee.BitrixUserId != 0)
                {
                    try
                    {
                        await _bitrixTimemanRepository.Do("close", employee.BitrixUserId);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(JsonConvert.SerializeObject(e));
                    }
                }

                shift.Ended = DateTime.UtcNow;
                await _shiftsRepository.UpdateAsync(shift);
                await _employeesRepository.UpdateEmployeeStatus(
                    employee.Id,
                    EmployeeStatus.WorkComplete
                );

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(JsonConvert.SerializeObject(e));
                return StatusCode(500);
            }
        }

        private async Task CheckCanEmployeeStartWorkingDay(Employee employee)
        {
            if (employee.Status != EmployeeStatus.WorkComplete)
                return;

            var shift = await _shiftsRepository
                .Entries.Where(s => s.EmployeeId == employee.Id)
                .OrderBy(s => s.Id)
                .LastOrDefaultAsync();
            if (shift == null)
                return;

            if (DateTime.Now - shift.Ended.ToLocalTime() < TimeSpan.FromHours(2))
                return;

            await _employeesRepository.UpdateEmployeeStatus(employee.Id, EmployeeStatus.Abscent);
        }

        private async Task<Employee?> GetCaller()
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
