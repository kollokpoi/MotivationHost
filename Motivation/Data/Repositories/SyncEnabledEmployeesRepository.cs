using Microsoft.EntityFrameworkCore;
using Motivation.Data.Repositories;
using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public interface ISyncEnabledEmployeesRepository : IRepository<Employee>
    {
        Task<bool> IsSyncEnabledAsync();
        Task<BitrixPortal?> GetCurrentPortalAsync();
        Task UpdateRange(IList<Employee> employees);
        Task UpdateEmployeeStatus(int employeeId, EmployeeStatus status);
    }

    public class SyncEnabledEmployeesRepository : ISyncEnabledEmployeesRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IBitrixSyncService _bitrixSyncService;
        private readonly ILogger<SyncEnabledEmployeesRepository> _logger;

        public SyncEnabledEmployeesRepository(
            ApplicationDbContext context,
            IBitrixSyncService bitrixSyncService,
            ILogger<SyncEnabledEmployeesRepository> logger
        )
        {
            _context = context;
            _bitrixSyncService = bitrixSyncService;
            _logger = logger;
        }

        public IQueryable<Employee> Entries =>
            _context.Employees
                .Include(e => e.Rank)
                .Include(e => e.Position)
                .Include(e => e.Qualification)
                .Include(e => e.Portal);

        public async Task CreateAsync(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            if (await IsSyncEnabledAsync())
            {
                var portal = await GetCurrentPortalAsync();
                if (portal != null)
                {
                    var action = employee.BitrixUserId > 0 ? "UPDATE" : "ADD";
                    var result = await _bitrixSyncService.SyncUserAsync(portal, employee, action);
                    
                    if (result?.Success == true && result.ExternalId.HasValue)
                    {
                        employee.BitrixUserId = result.ExternalId.Value;
                        await _context.SaveChangesAsync();
                    }
                    else if (result?.Success == false)
                    {
                        _logger.LogWarning($"Не удалось синхронизировать пользователя {employee.GetShortName()} с Bitrix: {result.Error}");
                    }
                }
            }
        }

        public async Task UpdateAsync(Employee employee)
        {
            var existingEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employee.Id);
            if (existingEmployee == null)
                return;

            // Сохраняем BitrixUserId если он был установлен
            if (existingEmployee.BitrixUserId > 0)
            {
                employee.BitrixUserId = existingEmployee.BitrixUserId;
            }

            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();

            if (await IsSyncEnabledAsync() && employee.BitrixUserId > 0)
            {
                var portal = await GetCurrentPortalAsync();
                if (portal != null)
                {
                    var result = await _bitrixSyncService.SyncUserAsync(portal, employee, "UPDATE");
                    
                    if (result?.Success == false)
                    {
                        _logger.LogWarning($"Не удалось обновить пользователя {employee.GetShortName()} в Bitrix: {result.Error}");
                    }
                }
            }
        }

        public async Task UpdateRange(IList<Employee> employees)
        {
            _context.Employees.UpdateRange(employees);
            await _context.SaveChangesAsync();

            if (await IsSyncEnabledAsync())
            {
                var portal = await GetCurrentPortalAsync();
                if (portal != null)
                {
                    foreach (var employee in employees)
                    {
                        if (employee.BitrixUserId > 0)
                        {
                            var result = await _bitrixSyncService.SyncUserAsync(portal, employee, "UPDATE");
                            
                            if (result?.Success == false)
                            {
                                _logger.LogWarning($"Не удалось обновить пользователя {employee.GetShortName()} в Bitrix: {result.Error}");
                            }
                        }
                    }
                }
            }
        }

        public async Task UpdateEmployeeStatus(int employeeId, EmployeeStatus status)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
            if (employee == null)
                return;

            employee.Status = status;
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();

            if (await IsSyncEnabledAsync() && employee.BitrixUserId > 0)
            {
                var portal = await GetCurrentPortalAsync();
                if (portal != null)
                {
                    var result = await _bitrixSyncService.SyncUserAsync(portal, employee, "UPDATE");
                    
                    if (result?.Success == false)
                    {
                        _logger.LogWarning($"Не удалось обновить статус пользователя {employee.GetShortName()} в Bitrix: {result.Error}");
                    }
                }
            }
        }

        public async Task DeleteAsync(int employeeId)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
            if (employee == null)
                return;

            if (await IsSyncEnabledAsync() && employee.BitrixUserId > 0)
            {
                var portal = await GetCurrentPortalAsync();
                if (portal != null)
                {
                    var result = await _bitrixSyncService.DeleteUserAsync(portal, employee.BitrixUserId);
                    
                    if (result?.Success == false)
                    {
                        _logger.LogWarning($"Не удалось удалить пользователя {employee.GetShortName()} из Bitrix: {result.Error}");
                    }
                }
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsSyncEnabledAsync()
        {
            var settings = await _context.BitrixSettings.FirstOrDefaultAsync();
            return settings?.Enabled == true && settings.SyncUsers;
        }

        public async Task<BitrixPortal?> GetCurrentPortalAsync()
        {
            return await _context.BitrixPortals.FirstOrDefaultAsync(p => p.IsActive);
        }
    }
}
