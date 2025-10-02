using InfoPoint.Models;
using System.ComponentModel.DataAnnotations;

namespace InfoPoint.Areas.LearningWalks.Models
{
    public class LearningWalkFeedback
    {
        public int Id { get; set; }
        
        [Required]
        public int AssignmentId { get; set; }
        public LearningWalkAssignment Assignment { get; set; } = null!;
        
        // Lesson Information
        public int? SelectedLessonEventId { get; set; }
        public string? LessonDetails { get; set; }
        public string? ObservationTiming { get; set; } // Start, Middle, End
        
        // Outcome Information
        public string? RoomOutcome { get; set; } // Yes, No
        public string? NoOutcomeDetails { get; set; } // No lesson found, Students present but no teacher, Found lesson in a different room
        
        // Learning Walk Feedback
        public string? FeedbackText { get; set; } // Observer's detailed feedback to teacher
        
        // Learning Walk Outcomes - Practice Levels
        public string? PlanningSequencing { get; set; } // Working towards, Core, Enhanced, Expert
        public string? LearningActivities { get; set; } // Working towards, Core, Enhanced, Expert
        public string? AssessmentChecks { get; set; } // Working towards, Core, Enhanced, Expert
        public string? BehavioursAttitudes { get; set; } // Working towards, Core (automatically set based on checklist)
        
        // Behaviours and Attitudes Checklist
        public string? BehaviourChecks { get; set; } // Comma-separated list of checked behaviours
        
        [Required]
        public string OverallRating { get; set; } = string.Empty; // Kept for backward compatibility
        
        public string? TeachingStrengths { get; set; }
        public string? AreasForImprovement { get; set; }
        public string? EngagementLevel { get; set; }
        public string? ClassroomBehavior { get; set; }
        public string? SupportNeeded { get; set; }
        public bool FollowUpRequired { get; set; }
        public string? AdditionalComments { get; set; }
        
        [Required]
        public DateTime SubmittedDate { get; set; }
        
        [Required]
        public string SubmittedById { get; set; } = string.Empty;
        public ApplicationUser SubmittedBy { get; set; } = null!;
        
        // Additional fields for future use
        public DateTime? LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
        public bool IsPublished { get; set; } = true; // Whether feedback is visible to observee
        public bool IsIncomplete { get; set; } = false; // Whether this is an incomplete observation
    }
}