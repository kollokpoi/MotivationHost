using Microsoft.EntityFrameworkCore;
using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public class EmployeePenaltiesRepository : IRepository<EmployeePenalty>
    {
        private readonly ApplicationDbContext _context;

        public EmployeePenaltiesRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<EmployeePenalty> Entries => _context.EmployeePenalties.Include(p => p.Penalty).Include(p => p.Author).Include(p => p.Employee);

        public async Task CreateAsync(EmployeePenalty employeePenalty)
        {
            _context.EmployeePenalties.Add(employeePenalty);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(EmployeePenalty employeePenalty)
        {
            var employeePenaltyExists = _context.EmployeePenalties.Any(d => d.Id == employeePenalty.Id);
            if (employeePenaltyExists)
            {
                _context.EmployeePenalties.Update(employeePenalty);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int employeePenaltyId)
        {
            var employeePenalty = _context.EmployeePenalties.FirstOrDefault(d => d.Id == employeePenaltyId);
            if (employeePenalty == null) return;
            _context.EmployeePenalties.Remove(employeePenalty);
            await _context.SaveChangesAsync();
        }
    }
}
