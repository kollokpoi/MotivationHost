using Microsoft.EntityFrameworkCore;
using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public class EmployeeTasksRepository : IRepository<EmployeeTask>
    {
        private readonly ApplicationDbContext _context;

        public EmployeeTasksRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<EmployeeTask> Entries => _context.EmployeeTasks.Include(t => t.Employee).Include(t => t.Author);

        public async Task CreateAsync(EmployeeTask task)
        {
            _context.EmployeeTasks.Add(task);
            await _context.SaveChangesAsync();
        }
     
        public async Task UpdateAsync(EmployeeTask task)
        {
            var taskExists = _context.EmployeeTasks.Any(t => t.Id == task.Id);
            if (taskExists)
            {
                _context.EmployeeTasks.Update(task);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int taskId)
        {
            var task = _context.EmployeeTasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) return;
            _context.EmployeeTasks.Remove(task);
            await _context.SaveChangesAsync();
        }
    }
}
