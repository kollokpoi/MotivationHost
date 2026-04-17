using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motivation.Data;
using Motivation.Models;
using Newtonsoft.Json;

namespace Motivation.Controllers
{
    [Authorize(Roles = "Admins")]
    public class PositionsController : Controller
    {
        private const int RanksCount = 11;
        private const int QualificationsCount = 3;
        private readonly Dictionary<int, string> Qualifications = new()
        {
            { 1, "Низкая" },
            { 2, "Средняя" },
            { 3, "Высокая" },
        };

        private readonly ILogger<PositionsController> _logger;
        private readonly IRepository<Position> _positionsRepository;
        private readonly IRepository<Qualification> _qualificationsRepository;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly IRepository<Rank> _rankRepository;

        public PositionsController(
            ILogger<PositionsController> logger,
            IRepository<Position> positionsRepository,
            IRepository<Qualification> qualificationsRepository,
            IEmployeesRepository employeesRepository,
            IRepository<Rank> rankRepository
        )
        {
            _logger = logger;
            _positionsRepository = positionsRepository;
            _qualificationsRepository = qualificationsRepository;
            _employeesRepository = employeesRepository;
            _rankRepository = rankRepository;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var positions = await _positionsRepository.Entries.OrderBy(p => p.Id).ToListAsync();
                return View(positions);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(500);
            }
        }

        [HttpPost]
        public async Task Create()
        {
            try
            {
                var position = await Request.ReadFromJsonAsync<Position>();
                if (position == null)
                    return;

                await _positionsRepository.CreateAsync(position);

                foreach (var qualificationNumber in Enumerable.Range(1, QualificationsCount))
                {
                    var qualification = new Qualification
                    {
                        Id = 0,
                        Name = Qualifications[qualificationNumber],
                        PositionId = position.Id,
                        Points = (qualificationNumber - 1) * 10,
                    };
                    await _qualificationsRepository.CreateAsync(qualification);
                }

                foreach (var rankNumber in Enumerable.Range(1, RanksCount))
                {
                    var rank = new Rank
                    {
                        Id = 0,
                        PositionId = position.Id,
                        Number = rankNumber,
                        SalaryBonus = 100,
                    };
                    await _rankRepository.CreateAsync(rank);
                }

                var json = JsonConvert.SerializeObject(
                    new { id = position.Id, updateTime = DateTime.Now.ToString() }
                );
                await Response.WriteAsync(json);

                _logger.LogInformation($"Position {position} created successfuly");
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying create position:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpPost]
        public async Task Update()
        {
            try
            {
                var position = await Request.ReadFromJsonAsync<Position>();
                if (position == null)
                    return;

                await _positionsRepository.UpdateAsync(position);

                var json = JsonConvert.SerializeObject(
                    new { updateTime = DateTime.Now.ToString() }
                );
                await Response.WriteAsync(json);

                _logger.LogInformation($"Position {position} updated successfuly");
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying create position:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpDelete]
        public async Task Delete()
        {
            try
            {
                var position = await Request.ReadFromJsonAsync<Position>();
                if (position == null)
                    return;

                var employees = _employeesRepository.Entries.Where(e =>
                    e.PositionId == position.Id
                );
                if (employees.Any())
                {
                    throw new Exception(
                        "Невозможно удалить данную должность! Существуют сотрудники с данной должностью!"
                    );
                }

                await _positionsRepository.DeleteAsync(position.Id);

                _logger.LogInformation($"Position {position} deleted successfully");
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying delete position:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }
    }
}
