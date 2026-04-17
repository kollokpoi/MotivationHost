using Microsoft.AspNetCore.Mvc;
using Motivation.Data;
using Motivation.Models;

[Route("[controller]")]
public class BonusController : Controller
{
    private readonly IRepository<Bonus> _bonusRepository;
    private readonly IRepository<BonusGradation> _gradationRepository;

    public BonusController(IRepository<Bonus> bonusRepository, IRepository<BonusGradation> gradationRepository)
    {
        _bonusRepository = bonusRepository;
        _gradationRepository = gradationRepository;
    }

    public IActionResult List(int positionId)
    {
        ViewBag.PositionId = positionId;
        var bonuses = _bonusRepository.Entries
            .Where(b => b.PositionId == positionId)
            .ToList();
        return View(bonuses);
    }

    [HttpGet("GetGradations/{bonusId}")]
    public IActionResult GetGradations(int bonusId)
    {
        var bonus = _bonusRepository.Entries.FirstOrDefault(b => b.Id == bonusId);

        return PartialView("_GradationsList", bonus);
    }

    [HttpGet("CreateForm")]
    public IActionResult CreateForm()
    {
        return PartialView("_BonusCreateRow");
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create([FromForm] string label, [FromForm] int positionId)
    {
        var bonus = new Bonus { Label = label, PositionId = positionId };
        await _bonusRepository.CreateAsync(bonus);
        return Ok();
    }

    [HttpGet("CreateGradationForm/{bonusId}")]
    public IActionResult CreateGradationForm(int bonusId)
    {
        return PartialView("_GradationCreateRow", bonusId);
    }

    [HttpPost("CreateGradation")]
    public async Task<IActionResult> CreateGradation([FromForm] int bonusId, [FromForm] string label, [FromForm] decimal price)
    {
        var gradation = new BonusGradation { BonusId = bonusId, Label = label, Price = price };
        await _gradationRepository.CreateAsync(gradation);
        return Ok();
    }

    [HttpPost("Update")]
    public async Task<IActionResult> Update([FromForm] int Id, [FromForm] string Label)
    {
        var bonus = _bonusRepository.Entries.FirstOrDefault(x=>x.Id == Id);
        if (bonus == null) return NotFound();

        bonus.Label = Label;
        await _bonusRepository.UpdateAsync(bonus);
        return Ok(new { updateTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
    }

    [HttpDelete("Delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
    {
        await _bonusRepository.DeleteAsync(request.Id);
        return Ok();
    }

    [HttpPost("UpdateGradation")]
    public async Task<IActionResult> UpdateGradation([FromForm] int Id, [FromForm] string Label, [FromForm] decimal Price)
    {
        var gradation = _gradationRepository.Entries.FirstOrDefault(x=>x.Id == Id);
        if (gradation == null) return NotFound();

        if (Label != null) gradation.Label = Label;
        gradation.Price = Price;

        await _gradationRepository.UpdateAsync(gradation);
        return Ok(new { updateTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
    }

    [HttpDelete("DeleteGradation")]
    public async Task<IActionResult> DeleteGradation([FromBody] DeleteRequest request)
    {
        await _gradationRepository.DeleteAsync(request.Id);
        return Ok();
    }
    public class UpdateGradationRequest { public int Id { get; set; } public string Label  { get; set; } = string.Empty; public decimal? Price { get; set; }  }
    public class DeleteRequest { public int Id { get; set; } }
}