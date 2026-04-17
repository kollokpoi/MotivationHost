using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public class PointsOfInterestRepository : IRepository<PointOfInterest>
    {
        private readonly ApplicationDbContext _context;

        public PointsOfInterestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<PointOfInterest> Entries => _context.PointsOfInterest;

        public async Task CreateAsync(PointOfInterest point)
        {
            _context.PointsOfInterest.Add(point);
            await _context.SaveChangesAsync();
        }
    
        public async Task UpdateAsync(PointOfInterest point)
        {
            var existedPoint = _context.PointsOfInterest.FirstOrDefault(p => p.Id == point.Id);
            if (existedPoint == null) return;

            if (!string.IsNullOrEmpty(point.Name))
            {
                existedPoint.Name = point.Name;
            }

            if (point.Longitude > 0)
            {
                existedPoint.Longitude = point.Longitude;
            }

            if (point.Latitude > 0)
            {
                existedPoint.Latitude = point.Latitude;
            }

            _context.PointsOfInterest.Update(existedPoint);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int pointId)
        {
            var point = _context.PointsOfInterest.FirstOrDefault(p => p.Id == pointId);
            if (point == null) return;
            _context.PointsOfInterest.Remove(point);
            await _context.SaveChangesAsync();
        }

    }
}
