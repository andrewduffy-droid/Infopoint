using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace InfoPoint.Models.Generated;

public partial class ApplicationDbContextTemp : DbContext
{
    public ApplicationDbContextTemp(DbContextOptions<ApplicationDbContextTemp> options)
        : base(options)
    {
    }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<BcVFullTimetable2025> BcVFullTimetable2025s { get; set; }

    public virtual DbSet<LearningWalkAssignment> LearningWalkAssignments { get; set; }

    public virtual DbSet<LearningWalkFeedback> LearningWalkFeedbacks { get; set; }

    public virtual DbSet<TermWindow> TermWindows { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex").IsUnique();
        });

        modelBuilder.Entity<AspNetRoleClaim>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.AspNetRoleClaims).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex").IsUnique();

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("AspNetUserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<BcVFullTimetable2025>(entity =>
        {
            entity.HasKey(e => e.EventId);

            entity.ToTable("bc_v_full_timetable_2025");

            entity.Property(e => e.EventId).HasColumnName("Event ID");
            entity.Property(e => e.EventName).HasColumnName("Event Name");
            entity.Property(e => e.ProgName).HasColumnName("Prog Name");
            entity.Property(e => e.ProgReference).HasColumnName("Prog Reference");
            entity.Property(e => e.Weekdayname).HasColumnName("weekdayname");
        });

        modelBuilder.Entity<LearningWalkAssignment>(entity =>
        {
            entity.HasIndex(e => e.ObserverId, "IX_LearningWalkAssignments_ObserverId");

            entity.HasIndex(e => e.StaffId, "IX_LearningWalkAssignments_StaffId");

            entity.HasOne(d => d.Observer).WithMany(p => p.LearningWalkAssignmentObservers)
                .HasForeignKey(d => d.ObserverId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Staff).WithMany(p => p.LearningWalkAssignmentStaffs)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LearningWalkFeedback>(entity =>
        {
            entity.HasIndex(e => e.AssignmentId, "IX_LearningWalkFeedbacks_AssignmentId");

            entity.HasIndex(e => e.SubmittedById, "IX_LearningWalkFeedbacks_SubmittedById");

            entity.HasOne(d => d.Assignment).WithMany(p => p.LearningWalkFeedbacks)
                .HasForeignKey(d => d.AssignmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.SubmittedBy).WithMany(p => p.LearningWalkFeedbacks)
                .HasForeignKey(d => d.SubmittedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
