using Microsoft.EntityFrameworkCore;
using Motivation.Data.Repositories;
using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public interface ISyncEnabledDepartmentsRepository : IRepository<Department>
    {
        Task<bool> IsSyncEnabledAsync();
        Task<BitrixPortal?> GetCurrentPortalAsync();
    }

    public class SyncEnabledDepartmentsRepository : ISyncEnabledDepartmentsRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IBitrixSyncService _bitrixSyncService;
        private readonly ILogger<SyncEnabledDepartmentsRepository> _logger;

        public SyncEnabledDepartmentsRepository(
            ApplicationDbContext context,
            IBitrixSyncService bitrixSyncService,
            ILogger<SyncEnabledDepartmentsRepository> logger
        )
        {
            _context = context;
            _bitrixSyncService = bitrixSyncService;
            _logger = logger;
        }

        public IQueryable<Department> Entries => _context.Departments.Include(d => d.Portal);

        public async Task CreateAsync(Department department)
        {
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            if (await IsSyncEnabledAsync())
            {
                var portal = await GetCurrentPortalAsync();
                if (portal != null)
                {
                    // Запускаем синхронизацию в фоне, не дожидаясь завершения
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var action = string.IsNullOrEmpty(department.ExternalId?.ToString()) ? "ADD" : "UPDATE";
                            var result = await _bitrixSyncService.SyncDepartmentAsync(portal, department, action);
                            
                            if (result?.Success == true && result.ExternalId.HasValue)
                            {
                                department.ExternalId = result.ExternalId.Value;
                                await _context.SaveChangesAsync();
                            }
                            else if (result?.Success == false)
                            {
                                _logger.LogWarning($"Не удалось синхронизировать подразделение {department.Name} с Bitrix: {result.Error}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Ошибка при фоновой синхронизации подразделения {department.Name} с Bitrix");
                        }
                    });
                }
            }
        }

        public async Task UpdateAsync(Department department)
        {
            var existingDepartment = await _context.Departments.FirstOrDefaultAsync(d => d.Id == department.Id);
            if (existingDepartment == null)
                return;

            // Обновляем ExternalId если он изменился
            if (!string.IsNullOrEmpty(existingDepartment.ExternalId?.ToString()))
            {
                department.ExternalId = existingDepartment.ExternalId;
            }

            _context.Departments.Update(department);
            await _context.SaveChangesAsync();

            if (await IsSyncEnabledAsync())
            {
                var portal = await GetCurrentPortalAsync();
                if (portal != null && department.ExternalId.HasValue)
                {
                    // Запускаем синхронизацию в фоне, не дожидаясь завершения
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var result = await _bitrixSyncService.SyncDepartmentAsync(portal, department, "UPDATE");
                            
                            if (result?.Success == false)
                            {
                                _logger.LogWarning($"Не удалось обновить подразделение {department.Name} в Bitrix: {result.Error}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Ошибка при фоновой синхронизации подразделения {department.Name} с Bitrix");
                        }
                    });
                }
            }
        }

        public async Task DeleteAsync(int departmentId)
        {
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId);
            if (department == null)
                return;

            if (await IsSyncEnabledAsync() && department.ExternalId.HasValue)
            {
                var portal = await GetCurrentPortalAsync();
                if (portal != null)
                {
                    // Запускаем синхронизацию в фоне, не дожидаясь завершения
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var result = await _bitrixSyncService.DeleteDepartmentAsync(portal, department.ExternalId.Value);
                            
                            if (result?.Success == false)
                            {
                                _logger.LogWarning($"Не удалось удалить подразделение {department.Name} из Bitrix: {result.Error}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Ошибка при фоновом удалении подразделения {department.Name} из Bitrix");
                        }
                    });
                }
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsSyncEnabledAsync()
        {
            var settings = await _context.BitrixSettings.FirstOrDefaultAsync();
            return settings?.Enabled == true && settings.SyncDepartments;
        }

        public async Task<BitrixPortal?> GetCurrentPortalAsync()
        {
            return await _context.BitrixPortals.FirstOrDefaultAsync(p => p.IsActive);
        }
    }
}
