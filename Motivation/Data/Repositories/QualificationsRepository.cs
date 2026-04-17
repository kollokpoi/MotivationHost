using Microsoft.EntityFrameworkCore;
using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public class QualificationsRepository : IRepository<Qualification>
    {
        private readonly ApplicationDbContext _context;

        public QualificationsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Qualification> Entries => _context.Qualifications;

        public async Task CreateAsync(Qualification qualification)
        {
            _context.Qualifications.Add(qualification);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Qualification qualification)
        {
            var existedQualification = _context.Qualifications.FirstOrDefault(q => q.Id == qualification.Id);
            if (existedQualification == null) return;

            if (!string.IsNullOrEmpty(qualification.Name))
            {
                existedQualification.Name = qualification.Name;
            }

            if (qualification.Points > 0)
            {
                existedQualification.Points = qualification.Points;
            }

            _context.Qualifications.Update(existedQualification);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int entryId)
        {
            var qualification = _context.Qualifications.FirstOrDefault(q => q.Id == entryId);
            if (qualification == null) return;
            _context.Qualifications.Remove(qualification);
            await _context.SaveChangesAsync();
        }
    }
}
