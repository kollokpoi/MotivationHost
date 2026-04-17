using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motivation.Data;
using Motivation.Data.Repositories;
using Motivation.Models;
using Motivation.ViewModels;
using Newtonsoft.Json;

namespace Motivation.Controllers
{
    [Authorize(Roles = "Admins,Managers")]
    public class PenaltiesController : Controller
    {
        private readonly ILogger<PenaltiesController> _logger;

        private readonly IRepository<Penalty> _penaltiesRepository;
        private readonly IRepository<EmployeePenalty> _employeePenaltiesRepository;
        private readonly IRepository<Position> _positionsRepository;

        public PenaltiesController(
            ILogger<PenaltiesController> logger,
            IRepository<Penalty> penaltiesRepository,
            IRepository<EmployeePenalty> employeePenaltiesRepository,
            IRepository<Position> positionsRepository
        )
        {
            _logger = logger;
            _penaltiesRepository = penaltiesRepository;
            _employeePenaltiesRepository = employeePenaltiesRepository;
            _positionsRepository = positionsRepository;
        }

        [HttpGet]
        [Authorize(Roles = "Admins,Managers")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var positions = await _positionsRepository.Entries.ToListAsync();
                var penalties = new List<Penalty>();
                if (positions.Any())
                {
                    var firstPositionId = positions.Min(p => p.Id);
                    penalties = await _penaltiesRepository
                        .Entries.Where(p => p.PositionId == firstPositionId)
                        .ToListAsync();
                }

                var penaltiesViewModel = new PenaltiesViewModel
                {
                    Penalties = penalties.OrderBy(p => p.Id).ToList(),
                    Positions = positions,
                };
                return View(penaltiesViewModel);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admins,Managers")]
        public async Task<IActionResult> Get(int positionId = 1)
        {
            var positions = await _positionsRepository.Entries.ToListAsync();
            var penalties = await _penaltiesRepository
                .Entries.Where(p => p.PositionId == positionId)
                .ToListAsync();
            var penaltiesViewModel = new PenaltiesViewModel
            {
                Penalties = penalties,
                Positions = positions,
            };
            return PartialView("_PenaltiesPartialView", penaltiesViewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admins,Managers")]
        public async Task Create()
        {
            try
            {
                var penalty = await Request.ReadFromJsonAsync<Penalty>();
                if (penalty == null)
                    return;

                await _penaltiesRepository.CreateAsync(penalty);

                await Response.WriteAsync(
                    JsonConvert.SerializeObject(
                        new { id = penalty.Id, updateTime = penalty.Updated.ToString() }
                    )
                );

                _logger.LogInformation($"Penalty {penalty} created successfuly");
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying create penalty:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                await Response.WriteAsync(
                    JsonConvert.SerializeObject(new { message = exceptionString })
                );
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admins,Managers")]
        public async Task Update()
        {
            try
            {
                var penalty = await Request.ReadFromJsonAsync<Penalty>();
                if (penalty == null)
                    return;

                await _penaltiesRepository.UpdateAsync(penalty);

                await Response.WriteAsync(
                    JsonConvert.SerializeObject(new { updateTime = DateTime.Now.ToString() })
                );

                _logger.LogInformation($"Penalty {penalty} updated successfuly");
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying create or update penalty:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                await Response.WriteAsync(
                    JsonConvert.SerializeObject(new { message = exceptionString })
                );
            }
        }

        [HttpDelete]
        [Authorize(Roles = "Admins,Managers")]
        public async Task Delete()
        {
            try
            {
                var penalty = await Request.ReadFromJsonAsync<Penalty>();
                if (penalty == null)
                    return;

                var employeePenalties = _employeePenaltiesRepository.Entries.Where(p =>
                    p.PenaltyId == penalty.Id
                );
                if (employeePenalties.Any())
                {
                    throw new Exception(
                        "Невозможно удалить данный штраф! Cуществуют созданные штрафы для сотрудников с таким типом."
                    );
                }

                await _penaltiesRepository.DeleteAsync(penalty.Id);

                _logger.LogInformation($"Penalty {penalty.Id} deleted successfully");
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying delete penalty:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                await Response.WriteAsync(
                    JsonConvert.SerializeObject(new { message = exceptionString })
                );
            }
        }

        [Route("api/v{version:apiVersion}/[controller]/List")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet]
        public async Task PositionPenalties(int positionId = 1)
        {
            try
            {
                var penalties = _penaltiesRepository.Entries.Where(p => p.PositionId == positionId);
                var response = await penalties
                    .Select(p => new { p.Id, p.Description })
                    .ToListAsync();
                var json = JsonConvert.SerializeObject(response);
                await Response.WriteAsync(json);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying get penalties:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                await Response.WriteAsync(
                    JsonConvert.SerializeObject(new { message = exceptionString })
                );
            }
        }
    }
}
