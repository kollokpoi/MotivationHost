using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motivation.Data;
using Motivation.Models;
using Motivation.ViewModels;
using Newtonsoft.Json;

namespace Motivation.Controllers
{
    [Authorize(Roles = "Admins, Managers")]
    public class QualificationsController : Controller
    {
        private readonly ILogger<QualificationsController> _logger;

        private readonly IRepository<Qualification> _qualificationsRepository;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly IRepository<Position> _positionsRepository;

        public QualificationsController(
            ILogger<QualificationsController> logger,
            IRepository<Qualification> qualificationsRepository,
            IEmployeesRepository employeesRepository,
            IRepository<Position> positionsRepository
        )
        {
            _logger = logger;
            _qualificationsRepository = qualificationsRepository;
            _employeesRepository = employeesRepository;
            _positionsRepository = positionsRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var positions = await _positionsRepository.Entries.OrderBy(p => p.Id).ToListAsync();
                var qualifications = new List<Qualification>();
                if (positions.Any())
                {
                    var firstPositionId = positions.Min(p => p.Id);
                    qualifications = await _qualificationsRepository
                        .Entries.Where(q => q.PositionId == firstPositionId)
                        .OrderBy(q => q.Id)
                        .ToListAsync();
                }

                var qualificationsViewModel = new QualificationsViewModel
                {
                    Qualifications = qualifications,
                    Positions = positions,
                };
                return View(qualificationsViewModel);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get(int positionId = 1)
        {
            var positions = await _positionsRepository.Entries.ToListAsync();
            var qualifications = await _qualificationsRepository
                .Entries.Where(q => q.PositionId == positionId)
                .ToListAsync();
            var qualificationsViewModel = new QualificationsViewModel
            {
                Qualifications = qualifications,
                Positions = positions,
            };
            return PartialView("_QualificationsPartialView", qualificationsViewModel);
        }

        [HttpGet]
        public async Task GetJson(int positionId = 1)
        {
            var positions = await _positionsRepository.Entries.ToListAsync();
            var qualifications = await _qualificationsRepository
                .Entries.Where(q => q.PositionId == positionId)
                .ToListAsync();
            var dict = new Dictionary<string, int>();
            foreach (var qualification in qualifications)
            {
                dict.Add(qualification.Name, qualification.Id);
            }

            await Response.WriteAsync(JsonConvert.SerializeObject(new { qualifications = dict }));
        }

        [HttpPost]
        public async Task Create()
        {
            try
            {
                var qualification = await Request.ReadFromJsonAsync<Qualification>();
                if (qualification == null)
                    return;

                await _qualificationsRepository.CreateAsync(qualification);

                await Response.WriteAsync(
                    JsonConvert.SerializeObject(
                        new { id = qualification.Id, updateTime = qualification.Updated.ToString() }
                    )
                );

                _logger.LogInformation($"Qualification {qualification} created successfuly");
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying create qualification:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                await Response.WriteAsync(
                    JsonConvert.SerializeObject(new { message = exceptionString })
                );
            }
        }

        [HttpPost]
        public async Task Update()
        {
            try
            {
                var qualification = await Request.ReadFromJsonAsync<Qualification>();
                if (qualification == null)
                    return;

                await _qualificationsRepository.UpdateAsync(qualification);

                await Response.WriteAsync(
                    JsonConvert.SerializeObject(new { updateTime = DateTime.Now.ToString() })
                );

                _logger.LogInformation($"Qualification {qualification} updated successfuly");
            }
            catch (Exception e)
            {
                var exceptionString =
                    $"Error occured while trying create or update qualification:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                await Response.WriteAsync(
                    JsonConvert.SerializeObject(new { message = exceptionString })
                );
            }
        }

        [HttpDelete]
        public async Task Delete()
        {
            try
            {
                var qualification = await Request.ReadFromJsonAsync<Qualification>();
                if (qualification == null)
                    return;

                var hasEmployees = await _employeesRepository
                    .Entries.Where(e => e.QualificationId == qualification.Id)
                    .AnyAsync();
                if (hasEmployees)
                {
                    throw new Exception(
                        "Невозможно удалить данную квалификацию! Существуют сотрудники с данной квалификацией!"
                    );
                }

                await _qualificationsRepository.DeleteAsync(qualification.Id);

                _logger.LogInformation($"Qualification {qualification.Id} deleted successfully");
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying delete qualification:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                await Response.WriteAsync(
                    JsonConvert.SerializeObject(new { message = exceptionString })
                );
            }
        }
    }
}
