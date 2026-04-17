using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public class RanksRepository : IRepository<Rank>
    {
        private readonly ApplicationDbContext _context;

        public RanksRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Rank> Entries => _context.Ranks;

        public async Task CreateAsync(Rank rank)
        {
            _context.Ranks.Add(rank);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Rank rank)
        {
            var existedRank = _context.Ranks.FirstOrDefault(d => d.Id == rank.Id);
            if (existedRank == null) return;

            if (rank.Number > 0)
            {
                existedRank.Number = rank.Number;
            }

            if (rank.SalaryBonus > 0)
            {
                existedRank.SalaryBonus = rank.SalaryBonus;
            }

            _context.Ranks.Update(existedRank);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int rankId)
        {
            var rank = _context.Ranks.FirstOrDefault(r => r.Id == rankId);
            if (rank == null) return;
            _context.Ranks.Remove(rank);
            await _context.SaveChangesAsync();
        }
    }
}
