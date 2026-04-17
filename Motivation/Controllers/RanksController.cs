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
    [Authorize(Roles = "Admins, Managers")]
    public class RanksController : Controller
    {
        private readonly ILogger<RanksController> _logger;

        private readonly IRepository<Rank> _ranksRepository;
        private readonly IRepository<Position> _positionsRepository;

        public RanksController(
            ILogger<RanksController> logger,
            IRepository<Rank> ranksRepository,
            IRepository<Position> positionsRepository
        )
        {
            _logger = logger;
            _ranksRepository = ranksRepository;
            _positionsRepository = positionsRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var positions = await _positionsRepository.Entries.OrderBy(p => p.Id).ToListAsync();
            var ranks = new List<Rank>();
            if (positions.Any())
            {
                var firstPositionId = positions.Min(p => p.Id);
                ranks = await _ranksRepository
                    .Entries.Where(r => r.PositionId == firstPositionId)
                    .OrderBy(q => q.Id)
                    .ToListAsync();
            }

            var ranksViewModel = new RanksViewModel { Ranks = ranks, Positions = positions };
            return View(ranksViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Get(int positionId = 1)
        {
            var positions = await _positionsRepository.Entries.ToListAsync();
            var ranks = await _ranksRepository
                .Entries.Where(r => r.PositionId == positionId)
                .ToListAsync();
            var ranksViewModel = new RanksViewModel { Ranks = ranks, Positions = positions };
            return PartialView("_RanksPartialView", ranksViewModel);
        }

        [HttpGet]
        public async Task GetJson(int positionId = 1)
        {
            var positions = await _positionsRepository.Entries.ToListAsync();
            var ranks = await _ranksRepository
                .Entries.Where(q => q.PositionId == positionId)
                .ToListAsync();
            var dict = new Dictionary<int, int>();
            foreach (var rank in ranks)
            {
                dict.Add(rank.Number, rank.Id);
            }

            await Response.WriteAsync(JsonConvert.SerializeObject(new { ranks = dict }));
        }

        [NonAction]
        [HttpPost]
        public async Task Create()
        {
            var rank = await Request.ReadFromJsonAsync<Rank>();
            if (rank == null)
                return;

            await _ranksRepository.CreateAsync(rank);

            await Response.WriteAsync(
                JsonConvert.SerializeObject(new { id = rank.Id, updateTime = rank.Updated })
            );
        }

        [HttpPost]
        public async Task Update()
        {
            var rank = await Request.ReadFromJsonAsync<Rank>();
            if (rank == null)
                return;

            await _ranksRepository.UpdateAsync(rank);

            await Response.WriteAsync(
                JsonConvert.SerializeObject(new { updateTime = DateTime.Now.ToString() })
            );
        }

        [NonAction]
        [HttpDelete]
        public async Task Delete()
        {
            var rank = await Request.ReadFromJsonAsync<Rank>();
            if (rank == null)
                return;
            await _ranksRepository.DeleteAsync(rank.Id);
        }
    }
}
