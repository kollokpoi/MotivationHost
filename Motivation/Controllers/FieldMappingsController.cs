using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motivation.Data.Repositories;
using Motivation.Models;

namespace Motivation.Controllers
{
    [Authorize]
    public class FieldMappingsController : Controller
    {
        private readonly IFieldMappingService _fieldMappingService;

        public FieldMappingsController(IFieldMappingService fieldMappingService)
        {
            _fieldMappingService = fieldMappingService;
        }

        /// <summary>
        /// Страница списка настроек маппинга для портала и сущности
        /// </summary>
        public async Task<IActionResult> Index(int portalId, string entityType = "Department")
        {
            ViewBag.PortalId = portalId;
            ViewBag.EntityType = entityType;
            ViewBag.EntityTypes = new[] { "Department", "Employee", "EmployeeTask" };

            var mappings = await _fieldMappingService.GetMappingsAsync(portalId, entityType);
            return View(mappings);
        }

        /// <summary>
        /// Страница добавления нового маппинга
        /// </summary>
        public IActionResult Create(int portalId, string entityType)
        {
            ViewBag.PortalId = portalId;
            ViewBag.EntityType = entityType;

            var mapping = new FieldMapping
            {
                PortalId = portalId,
                EntityType = entityType,
                IsActive = true,
                MappingType = "Direct"
            };

            return View(mapping);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FieldMapping mapping)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var mappings = new List<FieldMapping> { mapping };
                    await _fieldMappingService.SaveMappingsAsync(mappings);

                    return RedirectToAction(nameof(Index), new { portalId = mapping.PortalId, entityType = mapping.EntityType });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Ошибка сохранения: {ex.Message}");
                }
            }

            ViewBag.PortalId = mapping.PortalId;
            ViewBag.EntityType = mapping.EntityType;
            return View(mapping);
        }

        /// <summary>
        /// Страница редактирования маппинга
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            var mapping = await _fieldMappingService.GetByIdAsync(id);
            if (mapping == null)
            {
                return NotFound();
            }

            ViewBag.PortalId = mapping.PortalId;
            ViewBag.EntityType = mapping.EntityType;
            return View(mapping);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FieldMapping mapping)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var mappings = new List<FieldMapping> { mapping };
                    await _fieldMappingService.SaveMappingsAsync(mappings);

                    return RedirectToAction(nameof(Index), new { portalId = mapping.PortalId, entityType = mapping.EntityType });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Ошибка сохранения: {ex.Message}");
                }
            }

            return View(mapping);
        }

        /// <summary>
        /// Удаление маппинга
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int portalId, string entityType)
        {
            try
            {
                await _fieldMappingService.DeleteAsync(id);
                TempData["Success"] = "Маппинг удален";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка удаления: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { portalId, entityType });
        }
    }
}
