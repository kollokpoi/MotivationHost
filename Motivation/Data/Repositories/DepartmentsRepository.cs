using Microsoft.EntityFrameworkCore;
using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public class DepartmentsRepository : IRepository<Department>
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Department> Entries => _context.Departments;

        public async Task CreateAsync(Department department)
        {
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Department department)
        {
            var departmentExists = _context.Departments.Any(d => d.Id == department.Id);
            if (departmentExists)
            {
                _context.Departments.Update(department);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int departmentId)
        {
            var department = _context.Departments.FirstOrDefault(d => d.Id == departmentId);
            if (department == null) return;
            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
        }
    }
}
