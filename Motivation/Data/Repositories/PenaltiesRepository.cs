using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public class PenaltiesRepository : IRepository<Penalty>
    {
        private readonly ApplicationDbContext _context;

        public PenaltiesRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Penalty> Entries => _context.Penalties;

        public async Task CreateAsync(Penalty penalty)
        {
            _context.Penalties.Add(penalty);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Penalty penalty)
        {
            var existedPenalty = _context.Penalties.FirstOrDefault(p => p.Id == penalty.Id);
            if (existedPenalty == null) return;

            if (penalty.Points > 0)
            {
                existedPenalty.Points = penalty.Points;
            }

            if (!string.IsNullOrEmpty(penalty.Description))
            {
                existedPenalty.Description = penalty.Description;
            }

            _context.Penalties.Update(existedPenalty);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int penaltyId)
        {
            var penalty = _context.Penalties.FirstOrDefault(d => d.Id == penaltyId);
            if (penalty == null) return;
            _context.Penalties.Remove(penalty);
            await _context.SaveChangesAsync();
        }
    }
}
