using System;
using System.Collections.Generic;

namespace InfoPoint.Models.Generated;

public partial class AspNetUser
{
    public string Id { get; set; } = null!;

    public string? FullName { get; set; }

    public string? GoogleId { get; set; }

    public DateTime DateCreated { get; set; }

    public DateTime LastLoginDate { get; set; }

    public string? UserName { get; set; }

    public string? NormalizedUserName { get; set; }

    public string? Email { get; set; }

    public string? NormalizedEmail { get; set; }

    public int EmailConfirmed { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public string? PhoneNumber { get; set; }

    public int PhoneNumberConfirmed { get; set; }

    public int TwoFactorEnabled { get; set; }

    public string? LockoutEnd { get; set; }

    public int LockoutEnabled { get; set; }

    public int AccessFailedCount { get; set; }

    public int IsObserver { get; set; }

    public int? StaffReference { get; set; }

    public virtual ICollection<AspNetUserClaim> AspNetUserClaims { get; set; } = new List<AspNetUserClaim>();

    public virtual ICollection<AspNetUserLogin> AspNetUserLogins { get; set; } = new List<AspNetUserLogin>();

    public virtual ICollection<AspNetUserToken> AspNetUserTokens { get; set; } = new List<AspNetUserToken>();

    public virtual ICollection<LearningWalkAssignment> LearningWalkAssignmentObservers { get; set; } = new List<LearningWalkAssignment>();

    public virtual ICollection<LearningWalkAssignment> LearningWalkAssignmentStaffs { get; set; } = new List<LearningWalkAssignment>();

    public virtual ICollection<LearningWalkFeedback> LearningWalkFeedbacks { get; set; } = new List<LearningWalkFeedback>();

    public virtual ICollection<AspNetRole> Roles { get; set; } = new List<AspNetRole>();
}
