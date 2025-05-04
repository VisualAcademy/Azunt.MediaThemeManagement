using Microsoft.EntityFrameworkCore;

namespace Azunt.MediaThemeManagement
{
    public class MediaThemeAppDbContext : DbContext
    {
        public MediaThemeAppDbContext(DbContextOptions<MediaThemeAppDbContext> options)
            : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MediaTheme>()
                .Property(m => m.Created)
                .HasDefaultValueSql("GetDate()");
        }

        public DbSet<MediaTheme> MediaThemes { get; set; } = null!;
    }
}