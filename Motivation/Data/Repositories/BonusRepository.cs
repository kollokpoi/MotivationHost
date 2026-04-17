using Microsoft.EntityFrameworkCore;
using Motivation.Data;
using Motivation.Models;
using static iText.IO.Util.IntHashtable;

namespace Motivation.Data.Repositories
{
    public class BonusRepository : IRepository<Bonus>
    {
        private readonly ApplicationDbContext _context;

        public BonusRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Bonus> Entries => _context.Bonuses.Include(t => t.Gradations);

        public async Task CreateAsync(Bonus entry)
        {
            _context.Bonuses.Add(entry);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int entryId)
        {
            var item = _context.Bonuses.FirstOrDefault(x => x.Id == entryId);
            if (item is not null)
            {
                _context.Remove(item);
                await _context.SaveChangesAsync();
            }
                
        }

        public async Task UpdateAsync(Bonus entry)
        {
            _context.Update(entry);
            await _context.SaveChangesAsync();
        }
    }
}