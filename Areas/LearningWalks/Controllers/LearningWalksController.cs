using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InfoPoint.Data;
using InfoPoint.Models;
using InfoPoint.Areas.LearningWalks.Models;

namespace InfoPoint.Areas.LearningWalks.Controllers
{
    [Area("LearningWalks")]
    [Authorize]
    public class LearningWalksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TimetableDbContext _timetableContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public LearningWalksController(ApplicationDbContext context, TimetableDbContext timetableContext, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _timetableContext = timetableContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound();
            }

            // Get the current active term
            var activeTerm = await _context.TermWindows.FirstOrDefaultAsync(t => t.IsActive);
            var currentTerm = activeTerm?.Term ?? 1;

            // Get assignments where the current user is the observer (to conduct)
            var observerAssignments = await _context.LearningWalkAssignments
                .Include(a => a.Staff)
                .Where(a => a.ObserverId == currentUser.Id && a.IsActive && a.Term == currentTerm)
                .OrderBy(a => a.Staff.FullName)
                .ToListAsync();

            // Get assignments where the current user is being observed (to view feedback)
            var observeeAssignments = await _context.LearningWalkAssignments
                .Include(a => a.Observer)
                .Include(a => a.Staff)
                .Where(a => a.StaffId == currentUser.Id && a.IsActive && a.Term == currentTerm)
                .OrderBy(a => a.Observer.FullName)
                .ToListAsync();

            // Get feedback for observee assignments
            var observeeFeedback = new Dictionary<int, LearningWalkFeedback>();
            if (observeeAssignments.Any())
            {
                var assignmentIds = observeeAssignments.Select(a => a.Id).ToList();
                var feedbacks = await _context.LearningWalkFeedbacks
                    .Include(f => f.SubmittedBy)
                    .Where(f => assignmentIds.Contains(f.AssignmentId) && f.IsPublished)
                    .ToListAsync();
                
                observeeFeedback = feedbacks.ToDictionary(f => f.AssignmentId, f => f);
            }

            // Get feedback submitted by observer (to check completion status)
            var submittedFeedback = new HashSet<int>(); // Has at least one completed feedback
            var incompleteFeedback = new HashSet<int>(); // Has ONLY incomplete feedback(s), no completed ones
            if (observerAssignments.Any())
            {
                var observerAssignmentIds = observerAssignments.Select(a => a.Id).ToList();
                var feedbacks = await _context.LearningWalkFeedbacks
                    .Where(f => observerAssignmentIds.Contains(f.AssignmentId))
                    .Select(f => new { f.AssignmentId, f.IsIncomplete })
                    .ToListAsync();

                // Group by assignment to check if any completed feedback exists
                var feedbacksByAssignment = feedbacks.GroupBy(f => f.AssignmentId);

                foreach (var group in feedbacksByAssignment)
                {
                    // If any feedback is completed (not incomplete), mark as submitted
                    if (group.Any(f => !f.IsIncomplete))
                    {
                        submittedFeedback.Add(group.Key);
                    }
                    // Otherwise, if all feedbacks are incomplete, mark as incomplete
                    else if (group.All(f => f.IsIncomplete))
                    {
                        incompleteFeedback.Add(group.Key);
                    }
                }
            }

            ViewBag.CurrentTerm = currentTerm;
            ViewBag.IsObserver = currentUser.IsObserver;
            ViewBag.ObserverAssignments = observerAssignments;
            ViewBag.ObserveeAssignments = observeeAssignments;
            ViewBag.ObserveeFeedback = observeeFeedback;
            ViewBag.SubmittedFeedback = submittedFeedback;
            ViewBag.IncompleteFeedback = incompleteFeedback;
            ViewBag.IsBoth = currentUser.IsObserver && observeeAssignments.Any();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Conduct(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignment = await _context.LearningWalkAssignments
                .Include(a => a.Staff)
                .Include(a => a.Observer)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null)
            {
                return NotFound();
            }

            // Verify the current user is the assigned observer
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id != assignment.ObserverId)
            {
                return Forbid();
            }

            // Get timetable entries for the staff member
            var staffTimetable = new List<TimetableEntry>();
            if (assignment.Staff.StaffReference.HasValue)
            {
                var staffRefString = assignment.Staff.StaffReference.Value.ToString();
                
                // Get basic timetable entries first (without problematic date/time fields)
                staffTimetable = await _timetableContext.TimetableEntries
                    .Where(t => t.StaffReference == staffRefString)
                    .OrderBy(t => t.EventId)
                    .Take(100) // Limit results
                    .ToListAsync();

                // Now get the date/time data using raw SQL for each entry
                if (staffTimetable.Any())
                {
                    var eventIds = string.Join(",", staffTimetable.Select(t => t.EventId));
                    var sql = $@"
                        SELECT 
                            [Event ID] as EventId,
                            CAST(StartTime as NVARCHAR(50)) as StartTimeStr,
                            CAST(EndTime as NVARCHAR(50)) as EndTimeStr,
                            CAST(eventdate as NVARCHAR(50)) as EventDateStr
                        FROM bc_v_full_timetable_2025 
                        WHERE [Event ID] IN ({eventIds})";
                    
                    using var command = _timetableContext.Database.GetDbConnection().CreateCommand();
                    command.CommandText = sql;
                    await _timetableContext.Database.OpenConnectionAsync();
                    
                    using var reader = await command.ExecuteReaderAsync();
                    var timeData = new Dictionary<int, (string start, string end, string date)>();
                    
                    while (await reader.ReadAsync())
                    {
                        var eventId = reader.GetInt32(0); // EventId is first column
                        var startTime = reader.IsDBNull(1) ? "TBC" : reader.GetString(1); // StartTimeStr
                        var endTime = reader.IsDBNull(2) ? "TBC" : reader.GetString(2); // EndTimeStr
                        var eventDate = reader.IsDBNull(3) ? "TBC" : reader.GetString(3); // EventDateStr
                        
                        timeData[eventId] = (startTime, endTime, eventDate);
                    }
                    
                    // Update the timetable entries with the time data
                    foreach (var entry in staffTimetable)
                    {
                        if (timeData.TryGetValue(entry.EventId, out var times))
                        {
                            entry.StartTime = times.start;
                            entry.EndTime = times.end;
                            entry.EventDate = times.date;
                        }
                    }
                }
            }

            // Get existing feedback if any (only completed observations, not incomplete ones)
            var existingFeedback = await _context.LearningWalkFeedbacks
                .FirstOrDefaultAsync(f => f.AssignmentId == id && !f.IsIncomplete);

            // Get all incomplete observations for this assignment
            var incompleteObservations = await _context.LearningWalkFeedbacks
                .Include(f => f.SubmittedBy)
                .Where(f => f.AssignmentId == id && f.IsIncomplete)
                .OrderByDescending(f => f.SubmittedDate)
                .ToListAsync();

            ViewBag.StaffTimetable = staffTimetable;
            ViewBag.ExistingFeedback = existingFeedback;
            ViewBag.IncompleteObservations = incompleteObservations;

            return View(assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int assignmentId, int? selectedLessonEventId, string lessonDetails,
            string observationTiming, string roomOutcome, string noOutcomeDetails, string issueComments,
            string learningWalkFeedback, string planningSequencing, string learningActivities, string assessmentChecks,
            string behavioursAttitudes, string[] behaviourChecks, string overallRating,
            string teachingStrengths, string areasForImprovement, string engagementLevel, string classroomBehavior,
            string supportNeeded, bool followUpRequired, string additionalComments)
        {
            var assignment = await _context.LearningWalkAssignments.FindAsync(assignmentId);
            if (assignment == null)
            {
                return NotFound();
            }

            // Verify the current user is the assigned observer
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id != assignment.ObserverId)
            {
                return Forbid();
            }

            // Determine if this is an incomplete observation
            bool isIncompleteObservation = noOutcomeDetails == "No lesson found" ||
                                           noOutcomeDetails == "Students present but no teacher";

            // For incomplete observations, use issue comments as additional comments
            if (isIncompleteObservation && !string.IsNullOrEmpty(issueComments))
            {
                additionalComments = issueComments;
            }

            // Convert behaviour checks array to comma-separated string
            string behaviourChecksString = behaviourChecks != null ? string.Join(",", behaviourChecks) : "";

            // Set default overallRating based on outcomes (for backward compatibility)
            if (string.IsNullOrEmpty(overallRating))
            {
                overallRating = "See outcomes grid"; // Default value since it's required
            }

            // Check for existing feedback
            // For incomplete observations, always create a new entry (never update)
            // For completed observations, update if exists, otherwise create new
            LearningWalkFeedback? existingFeedback = null;

            if (isIncompleteObservation)
            {
                // For incomplete observations, check if a completed learning walk already exists
                var existingCompletedFeedback = await _context.LearningWalkFeedbacks
                    .FirstOrDefaultAsync(f => f.AssignmentId == assignmentId && !f.IsIncomplete);

                // If a completed learning walk exists, prevent submitting new incomplete observations
                if (existingCompletedFeedback != null)
                {
                    TempData["Error"] = "A completed learning walk already exists for this assignment. Incomplete observations can no longer be submitted.";
                    return RedirectToAction(nameof(Index));
                }
                // For incomplete observations, always create new entry (existingFeedback stays null)
            }
            else
            {
                // For completed observations, check if we're updating an existing one
                existingFeedback = await _context.LearningWalkFeedbacks
                    .FirstOrDefaultAsync(f => f.AssignmentId == assignmentId && !f.IsIncomplete);
            }

            if (existingFeedback != null)
            {
                // Update existing feedback
                existingFeedback.SelectedLessonEventId = selectedLessonEventId;
                existingFeedback.LessonDetails = lessonDetails;
                existingFeedback.ObservationTiming = observationTiming;
                existingFeedback.RoomOutcome = roomOutcome;
                existingFeedback.NoOutcomeDetails = noOutcomeDetails;
                existingFeedback.FeedbackText = learningWalkFeedback;
                existingFeedback.PlanningSequencing = planningSequencing;
                existingFeedback.LearningActivities = learningActivities;
                existingFeedback.AssessmentChecks = assessmentChecks;
                existingFeedback.BehavioursAttitudes = behavioursAttitudes;
                existingFeedback.BehaviourChecks = behaviourChecksString;
                existingFeedback.OverallRating = overallRating;
                existingFeedback.TeachingStrengths = teachingStrengths;
                existingFeedback.AreasForImprovement = areasForImprovement;
                existingFeedback.EngagementLevel = engagementLevel;
                existingFeedback.ClassroomBehavior = classroomBehavior;
                existingFeedback.SupportNeeded = supportNeeded;
                existingFeedback.FollowUpRequired = followUpRequired;
                existingFeedback.AdditionalComments = additionalComments;
                existingFeedback.LastModified = DateTime.UtcNow;
                existingFeedback.LastModifiedBy = currentUser.FullName;
                
                _context.Update(existingFeedback);
                TempData["Success"] = "Learning Walk feedback updated successfully!";
            }
            else
            {
                // Create new feedback
                var feedback = new LearningWalkFeedback
                {
                    AssignmentId = assignmentId,
                    SelectedLessonEventId = selectedLessonEventId,
                    LessonDetails = lessonDetails,
                    ObservationTiming = observationTiming,
                    RoomOutcome = roomOutcome,
                    NoOutcomeDetails = noOutcomeDetails,
                    FeedbackText = learningWalkFeedback,
                    PlanningSequencing = planningSequencing,
                    LearningActivities = learningActivities,
                    AssessmentChecks = assessmentChecks,
                    BehavioursAttitudes = behavioursAttitudes,
                    BehaviourChecks = behaviourChecksString,
                    OverallRating = overallRating,
                    TeachingStrengths = teachingStrengths,
                    AreasForImprovement = areasForImprovement,
                    EngagementLevel = engagementLevel,
                    ClassroomBehavior = classroomBehavior,
                    SupportNeeded = supportNeeded,
                    FollowUpRequired = followUpRequired,
                    AdditionalComments = additionalComments,
                    SubmittedDate = DateTime.UtcNow,
                    SubmittedById = currentUser.Id,
                    IsPublished = true,
                    IsIncomplete = isIncompleteObservation
                };

                _context.Add(feedback);
                
                if (isIncompleteObservation)
                {
                    TempData["Success"] = "Incomplete observation recorded. You can conduct another learning walk for this teacher.";
                }
                else
                {
                    TempData["Success"] = "Learning Walk completed successfully!";
                }
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ViewFeedback(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignment = await _context.LearningWalkAssignments
                .Include(a => a.Staff)
                .Include(a => a.Observer)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null)
            {
                return NotFound();
            }

            // Verify the current user is the staff member being observed
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id != assignment.StaffId)
            {
                return Forbid();
            }

            // Get completed feedback
            var feedback = await _context.LearningWalkFeedbacks
                .Include(f => f.SubmittedBy)
                .FirstOrDefaultAsync(f => f.AssignmentId == id && f.IsPublished && !f.IsIncomplete);

            if (feedback == null)
            {
                return NotFound("Feedback not yet available.");
            }

            // Get all incomplete observations for this assignment
            var incompleteObservations = await _context.LearningWalkFeedbacks
                .Include(f => f.SubmittedBy)
                .Where(f => f.AssignmentId == id && f.IsIncomplete)
                .OrderByDescending(f => f.SubmittedDate)
                .ToListAsync();

            ViewBag.Assignment = assignment;
            ViewBag.IncompleteObservations = incompleteObservations;
            return View(feedback);
        }
    }
}