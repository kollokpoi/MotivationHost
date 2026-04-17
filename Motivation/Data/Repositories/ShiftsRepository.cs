using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public class ShiftsRepository : IRepository<Shift>
    {
        private readonly ApplicationDbContext _context;

        public ShiftsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Shift> Entries => _context.Shifts;

        public async Task CreateAsync(Shift shift)
        {
            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Shift shift)
        {
            var existedShift = _context.Shifts.FirstOrDefault(d => d.Id == shift.Id);
            if (existedShift == null) return;
            _context.Shifts.Update(existedShift);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int shiftId)
        {
            var shift = _context.Shifts.FirstOrDefault(s => s.Id == shiftId);
            if (shift == null) return;
            _context.Shifts.Remove(shift);
            await _context.SaveChangesAsync();
        }

    }
}
