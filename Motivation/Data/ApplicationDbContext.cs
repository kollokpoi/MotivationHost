using Microsoft.EntityFrameworkCore;
using Motivation.Data.Repositories;
using Motivation.Models;
using System.Threading;

namespace Motivation.Data
{
    public class ApplicationDbContext : DbContext
    {
        private const int RanksCount = 11;
        private const int QualificationsCount = 3;
        private readonly Dictionary<int, string> QualificationNames = new()
        {
            { 1, "Низкая" },
            { 2, "Средняя" },
            { 3, "Высокая" },
        };

        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeePenalty> EmployeePenalties { get; set; }
        public DbSet<EmployeeTask> EmployeeTasks { get; set; }
        public DbSet<Penalty> Penalties { get; set; }
        public DbSet<PointOfInterest> PointsOfInterest { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Qualification> Qualifications { get; set; }
        public DbSet<Rank> Ranks { get; set; }
        public DbSet<ScoreSheet> ScoreSheets { get; set; }
        public DbSet<ShiftRule> ShiftRules { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Bonus> Bonuses { get; set; }
        public DbSet<BonusGradation> BonusGradations { get; set; }
        public DbSet<EmployeeBonus> EmployeeBonuses { get; set; }
        public DbSet<BitrixPortal> BitrixPortals { get; set; }
        public DbSet<BitrixSettings> BitrixSettings { get; set; }
        public DbSet<FieldMapping> FieldMappings { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            //Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public override int SaveChanges()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e =>
                    e.Entity is BaseEntity
                    && (e.State == EntityState.Added || e.State == EntityState.Modified)
                );

            foreach (var entityEntry in entries)
            {
                ((BaseEntity)entityEntry.Entity).Updated = DateTime.UtcNow;

                if (entityEntry.State == EntityState.Added)
                {
                    ((BaseEntity)entityEntry.Entity).Created = DateTime.UtcNow;
                }
            }

            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default
        )
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e =>
                    e.Entity is BaseEntity
                    && (e.State == EntityState.Added || e.State == EntityState.Modified)
                );

            foreach (var entityEntry in entries)
            {
                ((BaseEntity)entityEntry.Entity).Updated = DateTime.UtcNow;

                if (entityEntry.State == EntityState.Added)
                {
                    ((BaseEntity)entityEntry.Entity).Created = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

