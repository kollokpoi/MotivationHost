using Microsoft.EntityFrameworkCore;
using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public class BonusGradationRepository : IRepository<BonusGradation>
    {

        private readonly ApplicationDbContext _context;

        public BonusGradationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<BonusGradation> Entries => _context.BonusGradations.Include(t => t.Bonus);


        public async Task CreateAsync(BonusGradation entry)
        {
            _context.BonusGradations.Add(entry);
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

        public async Task UpdateAsync(BonusGradation entry)
        {
            _context.Update(entry);
            await _context.SaveChangesAsync();
        }
    }
}
