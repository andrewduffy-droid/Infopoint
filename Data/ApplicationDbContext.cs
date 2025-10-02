using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InfoPoint.Models;
using InfoPoint.Areas.LearningWalks.Models;

namespace InfoPoint.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<LearningWalkAssignment> LearningWalkAssignments { get; set; }
        public DbSet<TermWindow> TermWindows { get; set; }
        public DbSet<LearningWalkFeedback> LearningWalkFeedbacks { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<LearningWalkAssignment>()
                .HasOne(a => a.Observer)
                .WithMany()
                .HasForeignKey(a => a.ObserverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LearningWalkAssignment>()
                .HasOne(a => a.Staff)
                .WithMany()
                .HasForeignKey(a => a.StaffId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LearningWalkFeedback>()
                .HasOne(f => f.Assignment)
                .WithMany()
                .HasForeignKey(f => f.AssignmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LearningWalkFeedback>()
                .HasOne(f => f.SubmittedBy)
                .WithMany()
                .HasForeignKey(f => f.SubmittedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}