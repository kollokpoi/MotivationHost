using Microsoft.EntityFrameworkCore;
using Motivation.Data.Repositories;
using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public interface ISyncEnabledTasksRepository : IRepository<EmployeeTask>
    {
        Task<bool> IsSyncEnabledAsync();
        Task<BitrixPortal?> GetCurrentPortalAsync();
    }

    public class SyncEnabledTasksRepository : ISyncEnabledTasksRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IBitrixSyncService _bitrixSyncService;
        private readonly ILogger<SyncEnabledTasksRepository> _logger;

        public SyncEnabledTasksRepository(
            ApplicationDbContext context,
            IBitrixSyncService bitrixSyncService,
            ILogger<SyncEnabledTasksRepository> logger
        )
        {
            _context = context;
            _bitrixSyncService = bitrixSyncService;
            _logger = logger;
        }

        public IQueryable<EmployeeTask> Entries =>
            _context.EmployeeTasks
                .Include(t => t.Employee)
                .Include(t => t.Author)
                .Include(t => t.Portal);

        public async Task CreateAsync(EmployeeTask task)
        {
            _context.EmployeeTasks.Add(task);
            await _context.SaveChangesAsync();

            if (await IsSyncEnabledAsync())
            {
                var portal = await GetCurrentPortalAsync();
                if (portal != null && task.ExternalId.HasValue)
                {
                    var result = await _bitrixSyncService.SyncTaskAsync(portal, task, "ADD");
                    
                    if (result?.Success == true && result.ExternalId.HasValue)
                    {
                        task.ExternalId = result.ExternalId.Value;
                        await _context.SaveChangesAsync();
                    }
                    else if (result?.Success == false)
                    {
                        _logger.LogWarning($"Не удалось синхронизировать задачу {task.Title} с Bitrix: {result.Error}");
                    }
                }
            }
        }

        public async Task UpdateAsync(EmployeeTask task)
        {
            var existingTask = await _context.EmployeeTasks.FirstOrDefaultAsync(t => t.Id == task.Id);
            if (existingTask == null)
                return;

            // Сохраняем ExternalId если он был установлен
            if (existingTask.ExternalId.HasValue)
            {
                task.ExternalId = existingTask.ExternalId;
            }

            _context.EmployeeTasks.Update(task);
            await _context.SaveChangesAsync();

            if (await IsSyncEnabledAsync() && task.ExternalId.HasValue)
            {
                var portal = await GetCurrentPortalAsync();
                if (portal != null)
                {
                    var result = await _bitrixSyncService.SyncTaskAsync(portal, task, "UPDATE");
                    
                    if (result?.Success == false)
                    {
                        _logger.LogWarning($"Не удалось обновить задачу {task.Title} в Bitrix: {result.Error}");
                    }
                }
            }
        }

        public async Task DeleteAsync(int taskId)
        {
            var task = await _context.EmployeeTasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                return;

            if (await IsSyncEnabledAsync() && task.ExternalId.HasValue)
            {
                var portal = await GetCurrentPortalAsync();
                if (portal != null)
                {
                    var result = await _bitrixSyncService.DeleteTaskAsync(portal, task.ExternalId.Value);
                    
                    if (result?.Success == false)
                    {
                        _logger.LogWarning($"Не удалось удалить задачу {task.Title} из Bitrix: {result.Error}");
                    }
                }
            }

            _context.EmployeeTasks.Remove(task);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsSyncEnabledAsync()
        {
            var settings = await _context.BitrixSettings.FirstOrDefaultAsync();
            return settings?.Enabled == true && settings.SyncTasks;
        }

        public async Task<BitrixPortal?> GetCurrentPortalAsync()
        {
            return await _context.BitrixPortals.FirstOrDefaultAsync(p => p.IsActive);
        }
    }
}
