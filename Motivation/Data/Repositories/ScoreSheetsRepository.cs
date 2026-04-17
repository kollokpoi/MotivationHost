using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public class ScoreSheetsRepository : IRepository<ScoreSheet>
    {
        private readonly ApplicationDbContext _context;

        public ScoreSheetsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<ScoreSheet> Entries => _context.ScoreSheets;

        public async Task CreateAsync(ScoreSheet scoreSheet)
        {
            _context.ScoreSheets.Add(scoreSheet);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ScoreSheet scoreSheet)
        {
            var scoreSheetExists = _context.ScoreSheets.Any(s => s.Id == scoreSheet.Id);
            if (!scoreSheetExists) return;
            _context.ScoreSheets.Update(scoreSheet);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int scoreSheetId)
        {
            var scoreSheet = _context.ScoreSheets.FirstOrDefault(s => s.Id == scoreSheetId);
            if (scoreSheet == null) return;
            _context.ScoreSheets.Remove(scoreSheet);
            await _context.SaveChangesAsync();
        }
    }
}
