using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public class ShiftRulesRepository : IRepository<ShiftRule>
    {
        private readonly ApplicationDbContext _context;

        public ShiftRulesRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<ShiftRule> Entries => _context.ShiftRules;

        public async Task CreateAsync(ShiftRule rule)
        {
            _context.ShiftRules.Add(rule);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ShiftRule rule)
        {
            var ruleExists = _context.ShiftRules.Any(t => t.Id == rule.Id);
            if (ruleExists)
            {
                _context.ShiftRules.Update(rule);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int ruleId)
        {
            var rule = _context.ShiftRules.FirstOrDefault(t => t.Id == ruleId);
            if (rule == null) return;
            _context.ShiftRules.Remove(rule);
            await _context.SaveChangesAsync();
        }
    }
}
