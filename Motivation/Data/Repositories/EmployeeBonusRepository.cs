using Microsoft.EntityFrameworkCore;
using Motivation.Data;
using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public class EmployeeBonusRepository : IRepository<EmployeeBonus>
    {
        private readonly ApplicationDbContext _context;

        public EmployeeBonusRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<EmployeeBonus> Entries => _context.EmployeeBonuses.Include(t => t.BonusGradation).Include(x=>x.Employee);

        public async Task CreateAsync(EmployeeBonus entry)
        {
            _context.EmployeeBonuses.Add(entry);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int entryId)
        {
            var bonus = _context.EmployeeBonuses.FirstOrDefault(d => d.Id == entryId);
            if (bonus == null) return;
            _context.EmployeeBonuses.Remove(bonus);
            await _context.SaveChangesAsync();
        }

        public Task UpdateAsync(EmployeeBonus entry)
        {
            throw new NotImplementedException();
        }
    }
}