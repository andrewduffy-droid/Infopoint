using Microsoft.EntityFrameworkCore;
using InfoPoint.Models;

namespace InfoPoint.Data
{
    public class TimetableDbContext : DbContext
    {
        public TimetableDbContext(DbContextOptions<TimetableDbContext> options) : base(options)
        {
        }

        public DbSet<TimetableEntry> TimetableEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TimetableEntry>(entity =>
            {
                entity.ToTable("bc_v_full_timetable_2025");
                entity.HasKey(e => e.EventId);
                entity.Property(e => e.EventId).HasColumnName("Event ID");
                entity.Property(e => e.EventName).HasColumnName("Event Name");
                entity.Property(e => e.StaffReference).HasColumnName("StaffReference");
                entity.Property(e => e.StaffName).HasColumnName("StaffName");
                entity.Property(e => e.WeekdayName).HasColumnName("Weekdayname");
                entity.Ignore(e => e.ClassId); // Ignore ClassId due to data type mismatch
                entity.Ignore(e => e.StartTime); // Ignore due to DateTime casting issues
                entity.Ignore(e => e.EndTime); // Ignore due to DateTime casting issues  
                entity.Ignore(e => e.EventDate); // Ignore due to DateTime casting issues
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}