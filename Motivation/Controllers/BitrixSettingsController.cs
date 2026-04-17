using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motivation.Data;
using Motivation.Models;
using Motivation.ViewModels;

namespace Motivation.Controllers
{
    [Authorize(Roles = "Admins")]
    public class BitrixSettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BitrixSettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settings = await _context.BitrixSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new BitrixSettings();
                _context.BitrixSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            var portals = await _context.BitrixPortals.ToListAsync();
            var viewModel = new BitrixSettingsViewModel
            {
                Settings = settings,
                Portals = portals
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Update(BitrixSettings settings)
        {
            var existingSettings = await _context.BitrixSettings.FirstOrDefaultAsync();
            if (existingSettings == null)
            {
                _context.BitrixSettings.Add(settings);
            }
            else
            {
                existingSettings.Enabled = settings.Enabled;
                existingSettings.WebhookUrl = settings.WebhookUrl;
                existingSettings.SyncIntervalMinutes = settings.SyncIntervalMinutes;
                existingSettings.SyncTasks = settings.SyncTasks;
                existingSettings.SyncUsers = settings.SyncUsers;
                existingSettings.SyncDepartments = settings.SyncDepartments;
                existingSettings.SyncDeals = settings.SyncDeals;
                existingSettings.TwoWaySync = settings.TwoWaySync;
                existingSettings.UpdatedAt = DateTime.UtcNow;

                _context.BitrixSettings.Update(existingSettings);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Настройки синхронизации сохранены";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> AddPortal()
        {
            return View(new BitrixPortal());
        }

        [HttpPost]
        public async Task<IActionResult> AddPortal(BitrixPortal portal)
        {
            if (!ModelState.IsValid)
            {
                return View(portal);
            }

            // Если это первый портал, делаем его активным
            var hasActivePortal = await _context.BitrixPortals.AnyAsync(p => p.IsActive);
            if (!hasActivePortal)
            {
                portal.IsActive = true;
            }

            _context.BitrixPortals.Add(portal);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Портал добавлен";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> EditPortal(int id)
        {
            var portal = await _context.BitrixPortals.FindAsync(id);
            if (portal == null)
            {
                return NotFound();
            }

            return View(portal);
        }

        [HttpPost]
        public async Task<IActionResult> EditPortal(int id, BitrixPortal portal)
        {
            if (id != portal.Id)
            {
                return NotFound();
            }

            var existingPortal = await _context.BitrixPortals.FindAsync(id);
            if (existingPortal == null)
            {
                return NotFound();
            }

            existingPortal.Name = portal.Name;
            existingPortal.PortalUrl = portal.PortalUrl;
            existingPortal.WebhookUrl = portal.WebhookUrl;
            existingPortal.IncomingSecret = portal.IncomingSecret;
            
            // Если портал активируется, деактивируем остальные
            if (portal.IsActive)
            {
                var allPortals = await _context.BitrixPortals.ToListAsync();
                foreach (var p in allPortals)
                {
                    p.IsActive = (p.Id == id);
                }
            }
            
            existingPortal.UpdatedAt = DateTime.UtcNow;

            _context.BitrixPortals.Update(existingPortal);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Портал обновлен";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeletePortal(int id)
        {
            var portal = await _context.BitrixPortals.FindAsync(id);
            if (portal == null)
            {
                return NotFound();
            }

            _context.BitrixPortals.Remove(portal);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Портал удален";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> SetActivePortal(int id)
        {
            var portals = await _context.BitrixPortals.ToListAsync();
            foreach (var portal in portals)
            {
                portal.IsActive = portal.Id == id;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Активный портал изменен";
            return RedirectToAction(nameof(Index));
        }
    }
}
