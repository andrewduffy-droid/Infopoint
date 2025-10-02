using System;
using System.Collections.Generic;

namespace InfoPoint.Models.Generated;

public partial class LearningWalkFeedback
{
    public int Id { get; set; }

    public int AssignmentId { get; set; }

    public string OverallRating { get; set; } = null!;

    public string? TeachingStrengths { get; set; }

    public string? AreasForImprovement { get; set; }

    public string? EngagementLevel { get; set; }

    public string? ClassroomBehavior { get; set; }

    public string? SupportNeeded { get; set; }

    public int FollowUpRequired { get; set; }

    public string? AdditionalComments { get; set; }

    public string SubmittedDate { get; set; } = null!;

    public string SubmittedById { get; set; } = null!;

    public string? LastModified { get; set; }

    public string? LastModifiedBy { get; set; }

    public int IsPublished { get; set; }

    public string? LessonDetails { get; set; }

    public string? ObservationTiming { get; set; }

    public int? SelectedLessonEventId { get; set; }

    public virtual LearningWalkAssignment Assignment { get; set; } = null!;

    public virtual AspNetUser SubmittedBy { get; set; } = null!;
}
