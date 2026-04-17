using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motivation.Data;
using Motivation.Models;
using Motivation.ViewModels;
using Newtonsoft.Json;

namespace Motivation.Controllers
{
    [Authorize(Roles = "Admins")]
    public class PointsOfInterestController : Controller
    {
        private readonly ILogger<PointsOfInterestController> _logger;

        private readonly IRepository<PointOfInterest> _pointsOfInteresRepository;

        public PointsOfInterestController(
            ILogger<PointsOfInterestController> logger,
            IRepository<PointOfInterest> pointsOfInteresRepository
        )
        {
            _logger = logger;
            _pointsOfInteresRepository = pointsOfInteresRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var points = await _pointsOfInteresRepository.Entries.ToListAsync();
            var pointsViewModel = new PointsOfInterestViewModel
            {
                Points = points.OrderBy(p => p.Id).ToList(),
            };
            return View(pointsViewModel);
        }

        [HttpPost]
        public async Task Create()
        {
            try
            {
                var point = await Request.ReadFromJsonAsync<PointOfInterest>();
                if (point == null)
                    return;

                await _pointsOfInteresRepository.CreateAsync(point);

                var json = JsonConvert.SerializeObject(
                    new { id = point.Id, updateTime = DateTime.Now.ToString() }
                );
                await Response.WriteAsync(json);

                _logger.LogInformation($"PointOfInterest {point} created successfuly");
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying create PointOfInterest:\n {e}";
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
                var point = await Request.ReadFromJsonAsync<PointOfInterest>();
                if (point == null)
                    return;

                await _pointsOfInteresRepository.UpdateAsync(point);

                var json = JsonConvert.SerializeObject(
                    new { updateTime = DateTime.Now.ToString() }
                );
                await Response.WriteAsync(json);

                _logger.LogInformation($"PointOfInterest {point} updated successfuly");
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying create pointOfInterest:\n {e}";
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
                var point = await Request.ReadFromJsonAsync<PointOfInterest>();
                if (point == null)
                    return;
                await _pointsOfInteresRepository.DeleteAsync(point.Id);

                _logger.LogInformation($"PointOfInterest {point} deleted successfully");
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying delete pointOfInterest:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }
    }
}
