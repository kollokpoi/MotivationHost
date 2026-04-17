using Microsoft.EntityFrameworkCore;
using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public class PositionsRepository : IRepository<Position>
    {
        private readonly ApplicationDbContext _context;

        public PositionsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Position> Entries => _context.Positions;

        public async Task CreateAsync(Position position)
        {
            _context.Positions.Add(position);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Position position)
        {
            var existedPosition = _context.Positions.FirstOrDefault(d => d.Id == position.Id);
            if (existedPosition == null) return;

            if (!string.IsNullOrEmpty(position.Name))
            {
                existedPosition.Name = position.Name;
            }

            if (position.Salary > 0)
            {
                existedPosition.Salary = position.Salary;
            }

            _context.Positions.Update(existedPosition);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int positionId)
        {
            var position = _context.Positions.FirstOrDefault(p => p.Id == positionId);
            if (position == null) return;
            _context.Positions.Remove(position);
            await _context.SaveChangesAsync();
        }
    }
}
