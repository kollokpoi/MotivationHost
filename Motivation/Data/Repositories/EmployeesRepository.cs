using Microsoft.EntityFrameworkCore;
using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public class EmployeesRepository : IEmployeesRepository
    {
        private readonly ApplicationDbContext _context;

        public EmployeesRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Employee> Entries => _context.Employees.Include(e => e.Rank).Include(e => e.Position).Include(e => e.Qualification);

        public async Task CreateAsync(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Employee employee)
        {
            var employeeExists = _context.Employees.Any(d => d.Id == employee.Id);
            if (!employeeExists) return;
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRange(IList<Employee> employees)
        {
            _context.Employees.UpdateRange(employees);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateEmployeeStatus(int employeeId, EmployeeStatus status)
        {
            var employee = _context.Employees.FirstOrDefault(d => d.Id == employeeId);
            if (employee == null) return;
            employee.Status = status;
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int employeeId)
        {
            var employee = _context.Employees.FirstOrDefault(d => d.Id == employeeId);
            if (employee == null) return;
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
        }
    }
}
