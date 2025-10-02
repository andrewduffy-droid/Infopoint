using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InfoPoint.Models;
using InfoPoint.Data;
using InfoPoint.Areas.LearningWalks.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using InfoPoint.Services;

namespace InfoPoint.Areas.LearningWalks.Controllers
{
    [Area("LearningWalks")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TimetableDbContext _timetableContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StaffSyncService _staffSyncService;

        public AdminController(ApplicationDbContext context, TimetableDbContext timetableContext, UserManager<ApplicationUser> userManager, StaffSyncService staffSyncService)
        {
            _context = context;
            _timetableContext = timetableContext;
            _userManager = userManager;
            _staffSyncService = staffSyncService;
        }

        public async Task<IActionResult> Index(int? term, string? observerId, string? searchStaff)
        {
            // Get term windows
            var termWindows = await _context.TermWindows.ToListAsync();
            ViewBag.TermWindows = termWindows;

            // If no term specified, use the first active term or default to Term 1
            if (!term.HasValue)
            {
                var activeWindow = termWindows.FirstOrDefault(t => t.IsActive);
                term = activeWindow?.Term ?? 1;
            }

            ViewBag.CurrentTerm = term.Value;
            ViewBag.CurrentObserverId = observerId;
            ViewBag.SearchStaff = searchStaff;

            // Build assignments query
            var assignmentsQuery = _context.LearningWalkAssignments
                .Include(a => a.Observer)
                .Include(a => a.Staff)
                .Where(a => a.IsActive && a.Term == term.Value);

            // Apply observer filter
            if (!string.IsNullOrEmpty(observerId))
            {
                assignmentsQuery = assignmentsQuery.Where(a => a.ObserverId == observerId);
            }

            var assignments = await assignmentsQuery
                .OrderBy(a => a.Staff.FullName)
                .ToListAsync();

            // Get all users
            var allUsersQuery = _userManager.Users.AsQueryable();

            // Apply staff search filter
            if (!string.IsNullOrEmpty(searchStaff))
            {
                allUsersQuery = allUsersQuery.Where(u => 
                    u.FullName.Contains(searchStaff) || 
                    u.Email.Contains(searchStaff));
            }

            var allUsers = await allUsersQuery
                .OrderBy(u => u.FullName)
                .ToListAsync();

            // Get feedback status for all assignments
            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var feedbacks = await _context.LearningWalkFeedbacks
                .Where(f => assignmentIds.Contains(f.AssignmentId))
                .Select(f => new { f.AssignmentId, f.IsIncomplete })
                .ToListAsync();

            // Group feedback by assignment to get status and incomplete count
            var feedbackByAssignment = feedbacks.GroupBy(f => f.AssignmentId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        HasCompleted = g.Any(f => !f.IsIncomplete),
                        IncompleteCount = g.Count(f => f.IsIncomplete)
                    }
                );

            var staffWithAssignments = allUsers.Select(user =>
            {
                var assignment = assignments.FirstOrDefault(a => a.StaffId == user.Id);
                var feedbackStatus = assignment != null && feedbackByAssignment.ContainsKey(assignment.Id)
                    ? feedbackByAssignment[assignment.Id]
                    : null;

                return new
                {
                    User = user,
                    Assignment = assignment,
                    HasCompleted = feedbackStatus?.HasCompleted ?? false,
                    IncompleteCount = feedbackStatus?.IncompleteCount ?? 0
                };
            }).ToList();

            ViewBag.StaffWithAssignments = staffWithAssignments;
            
            // Get only designated observers for filter dropdown
            var allObservers = await _userManager.Users
                .Where(u => u.IsObserver)
                .OrderBy(u => u.FullName)
                .ToListAsync();
            ViewBag.ObserverList = new SelectList(allObservers, "Id", "FullName", observerId);
            ViewBag.CurrentObservers = allObservers;

            return View(assignments);
        }

        public async Task<IActionResult> Create()
        {
            var observers = await _userManager.Users
                .Where(u => u.IsObserver)
                .OrderBy(u => u.FullName)
                .ToListAsync();
            
            var allUsers = await _userManager.Users
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.ObserverList = new SelectList(observers, "Id", "FullName");
            ViewBag.StaffList = new SelectList(allUsers, "Id", "FullName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LearningWalkAssignment assignment, int currentTerm)
        {
            if (assignment.ObserverId == assignment.StaffId)
            {
                ModelState.AddModelError("", "Observer and Staff cannot be the same person.");
            }

            // Validate that the selected observer is actually designated as an observer
            var observer = await _userManager.FindByIdAsync(assignment.ObserverId);
            if (observer == null || !observer.IsObserver)
            {
                ModelState.AddModelError("", "Selected user is not designated as an observer.");
            }

            assignment.Term = currentTerm;
            
            var existingAssignment = await _context.LearningWalkAssignments
                .FirstOrDefaultAsync(a => a.ObserverId == assignment.ObserverId 
                    && a.StaffId == assignment.StaffId 
                    && a.Term == assignment.Term
                    && a.IsActive);

            if (existingAssignment != null)
            {
                ModelState.AddModelError("", "This assignment already exists for this term.");
            }

            if (ModelState.IsValid)
            {
                assignment.AssignedDate = DateTime.UtcNow;
                assignment.IsActive = true;
                _context.Add(assignment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Assignment created successfully.";
                return RedirectToAction(nameof(Index), new { term = currentTerm, observerId = Request.Query["observerId"], searchStaff = Request.Query["searchStaff"] });
            }

            var observers = await _userManager.Users.Where(u => u.IsObserver).OrderBy(u => u.FullName).ToListAsync();
            var allUsers = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
            ViewBag.ObserverList = new SelectList(observers, "Id", "FullName", assignment.ObserverId);
            ViewBag.StaffList = new SelectList(allUsers, "Id", "FullName", assignment.StaffId);
            return View(assignment);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignment = await _context.LearningWalkAssignments
                .Include(a => a.Observer)
                .Include(a => a.Staff)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (assignment == null)
            {
                return NotFound();
            }

            return View(assignment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var assignment = await _context.LearningWalkAssignments.FindAsync(id);
            if (assignment != null)
            {
                assignment.IsActive = false;
                _context.Update(assignment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Assignment removed successfully.";
            }
            return RedirectToAction(nameof(Index), new { term = Request.Query["term"], observerId = Request.Query["observerId"], searchStaff = Request.Query["searchStaff"] });
        }

        public async Task<IActionResult> BulkAssign()
        {
            var observers = await _userManager.Users
                .Where(u => u.IsObserver)
                .OrderBy(u => u.FullName)
                .ToListAsync();
            
            var allUsers = await _userManager.Users
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.ObserverList = new SelectList(observers, "Id", "FullName");
            ViewBag.StaffList = new MultiSelectList(allUsers, "Id", "FullName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAssign(string observerId, List<string> staffIds, int currentTerm)
        {
            if (string.IsNullOrEmpty(observerId) || staffIds == null || !staffIds.Any())
            {
                TempData["Error"] = "Please select an observer and at least one staff member.";
                return RedirectToAction(nameof(BulkAssign));
            }

            // Validate observer
            var observer = await _userManager.FindByIdAsync(observerId);
            if (observer == null || !observer.IsObserver)
            {
                TempData["Error"] = "Selected user is not designated as an observer.";
                return RedirectToAction(nameof(BulkAssign));
            }

            if (staffIds.Contains(observerId))
            {
                TempData["Error"] = "Observer cannot be assigned to themselves.";
                return RedirectToAction(nameof(BulkAssign));
            }

            var successCount = 0;
            var skipCount = 0;

            foreach (var staffId in staffIds)
            {
                var existingAssignment = await _context.LearningWalkAssignments
                    .FirstOrDefaultAsync(a => a.ObserverId == observerId 
                        && a.StaffId == staffId 
                        && a.Term == currentTerm
                        && a.IsActive);

                if (existingAssignment == null)
                {
                    var assignment = new LearningWalkAssignment
                    {
                        ObserverId = observerId,
                        StaffId = staffId,
                        Term = currentTerm,
                        AssignedDate = DateTime.UtcNow,
                        IsActive = true
                    };

                    _context.Add(assignment);
                    successCount++;
                }
                else
                {
                    skipCount++;
                }
            }

            if (successCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Successfully created {successCount} assignments. {(skipCount > 0 ? $"Skipped {skipCount} existing assignments." : "")}";
            return RedirectToAction(nameof(Index), new { term = currentTerm });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeObserver(string staffId, string newObserverId, int currentTerm)
        {
            if (string.IsNullOrEmpty(staffId) || string.IsNullOrEmpty(newObserverId))
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction(nameof(Index), new { term = currentTerm, observerId = Request.Query["observerId"], searchStaff = Request.Query["searchStaff"] });
            }

            // Validate observer
            var observer = await _userManager.FindByIdAsync(newObserverId);
            if (observer == null || !observer.IsObserver)
            {
                TempData["Error"] = "Selected user is not designated as an observer.";
                return RedirectToAction(nameof(Index), new { term = currentTerm, observerId = Request.Query["observerId"], searchStaff = Request.Query["searchStaff"] });
            }

            if (staffId == newObserverId)
            {
                TempData["Error"] = "Staff member cannot observe themselves.";
                return RedirectToAction(nameof(Index), new { term = currentTerm, observerId = Request.Query["observerId"], searchStaff = Request.Query["searchStaff"] });
            }

            var existingAssignment = await _context.LearningWalkAssignments
                .FirstOrDefaultAsync(a => a.StaffId == staffId && a.Term == currentTerm && a.IsActive);

            if (existingAssignment != null)
            {
                existingAssignment.ObserverId = newObserverId;
                existingAssignment.AssignedDate = DateTime.UtcNow;
                _context.Update(existingAssignment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Observer changed successfully.";
            }
            else
            {
                var newAssignment = new LearningWalkAssignment
                {
                    ObserverId = newObserverId,
                    StaffId = staffId,
                    Term = currentTerm,
                    AssignedDate = DateTime.UtcNow,
                    IsActive = true
                };
                _context.Add(newAssignment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Observer assigned successfully.";
            }

            return RedirectToAction(nameof(Index), new { term = currentTerm });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLearningWalk(string staffId, bool enable, int currentTerm)
        {
            if (string.IsNullOrEmpty(staffId))
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction(nameof(Index), new { term = currentTerm, observerId = Request.Query["observerId"], searchStaff = Request.Query["searchStaff"] });
            }

            var assignment = await _context.LearningWalkAssignments
                .FirstOrDefaultAsync(a => a.StaffId == staffId && a.Term == currentTerm && a.IsActive);

            if (enable && assignment == null)
            {
                TempData["Error"] = "Cannot enable learning walk without an observer. Please assign an observer first.";
            }
            else if (!enable && assignment != null)
            {
                assignment.IsActive = false;
                _context.Update(assignment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Learning walk disabled successfully.";
            }
            else if (enable && assignment != null)
            {
                assignment.IsActive = true;
                _context.Update(assignment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Learning walk enabled successfully.";
            }

            return RedirectToAction(nameof(Index), new { term = currentTerm });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTermWindow(int termNumber, bool activate)
        {
            var termWindow = await _context.TermWindows.FirstOrDefaultAsync(t => t.Term == termNumber);
            
            if (termWindow != null)
            {
                if (activate)
                {
                    // Deactivate all other terms
                    var allTerms = await _context.TermWindows.ToListAsync();
                    foreach (var t in allTerms)
                    {
                        t.IsActive = false;
                    }
                }
                
                termWindow.IsActive = activate;
                termWindow.LastModified = DateTime.UtcNow;
                termWindow.ModifiedBy = User.Identity?.Name ?? "System";
                
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Term {termNumber} {(activate ? "activated" : "deactivated")} successfully.";
            }

            return RedirectToAction(nameof(Index), new { term = termNumber });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleObserver(string userId, bool makeObserver)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsObserver = makeObserver;
                var result = await _userManager.UpdateAsync(user);
                
                if (result.Succeeded)
                {
                    TempData["Success"] = $"{user.FullName} {(makeObserver ? "added as" : "removed from")} observer role successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to update observer status.";
                }
            }

            return RedirectToAction(nameof(Index), new { 
                term = Request.Query["term"], 
                observerId = Request.Query["observerId"], 
                searchStaff = Request.Query["searchStaff"] 
            });
        }

        public async Task<IActionResult> ManageObservers()
        {
            var allUsers = await _userManager.Users
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(allUsers);
        }

        public async Task<IActionResult> StaffSync()
        {
            // Get current stats - use simpler queries to avoid timeout
            var totalUsers = await _userManager.Users.CountAsync();
            var usersWithStaffRef = await _userManager.Users.CountAsync(u => u.StaffReference.HasValue);
            
            // Use a much simpler query for timetable count to avoid timeout
            // Just get a sample of records to verify connection
            int timetableStaffCount;
            try
            {
                // Try to get a small sample first to test connection
                var sampleEntries = await _timetableContext.TimetableEntries
                    .Where(t => !string.IsNullOrEmpty(t.StaffReference) && !string.IsNullOrEmpty(t.StaffName))
                    .Take(100)
                    .Select(t => t.StaffReference)
                    .ToListAsync();
                
                // If we got data, estimate based on sample
                if (sampleEntries.Any())
                {
                    var distinctSample = sampleEntries.Distinct().Count();
                    timetableStaffCount = distinctSample; // Show sample count for now
                }
                else
                {
                    timetableStaffCount = 0;
                }
            }
            catch (Exception ex)
            {
                // If query fails, show error and set count to -1
                TempData["Error"] = $"Unable to connect to timetable database: {ex.Message}";
                timetableStaffCount = -1;
            }

            ViewBag.TotalUsers = totalUsers;
            ViewBag.UsersWithStaffRef = usersWithStaffRef;
            ViewBag.TimetableStaffCount = timetableStaffCount;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncStaffFromTimetable()
        {
            var result = await _staffSyncService.SyncStaffFromTimetableAsync();
            
            if (result.Success)
            {
                TempData["Success"] = $"Staff sync completed successfully. {result.Summary}";
            }
            else
            {
                TempData["Error"] = $"Staff sync completed with errors. {result.Summary}. Errors: {string.Join("; ", result.Errors)}";
            }

            return RedirectToAction(nameof(StaffSync));
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

            // Get completed feedback
            var feedback = await _context.LearningWalkFeedbacks
                .Include(f => f.SubmittedBy)
                .FirstOrDefaultAsync(f => f.AssignmentId == id && f.IsPublished && !f.IsIncomplete);

            if (feedback == null)
            {
                TempData["Error"] = "No completed feedback available for this assignment.";
                return RedirectToAction(nameof(Index));
            }

            // Get all incomplete observations for this assignment
            var incompleteObservations = await _context.LearningWalkFeedbacks
                .Include(f => f.SubmittedBy)
                .Where(f => f.AssignmentId == id && f.IsIncomplete)
                .OrderByDescending(f => f.SubmittedDate)
                .ToListAsync();

            ViewBag.Assignment = assignment;
            ViewBag.IncompleteObservations = incompleteObservations;
            ViewBag.IsAdminView = true;

            return View("~/Areas/LearningWalks/Views/LearningWalks/ViewFeedback.cshtml", feedback);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTermDates(int termNumber, DateTime? startDate, DateTime? endDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Get or create the term window
            var termWindow = await _context.TermWindows.FirstOrDefaultAsync(t => t.Term == termNumber);

            if (termWindow == null)
            {
                // Create new term window if it doesn't exist
                termWindow = new TermWindow
                {
                    Term = termNumber,
                    IsActive = false,
                    StartDate = startDate,
                    EndDate = endDate,
                    LastModified = DateTime.UtcNow,
                    ModifiedBy = currentUser?.FullName ?? "Admin"
                };
                _context.TermWindows.Add(termWindow);
            }
            else
            {
                // Update existing term window
                termWindow.StartDate = startDate;
                termWindow.EndDate = endDate;
                termWindow.LastModified = DateTime.UtcNow;
                termWindow.ModifiedBy = currentUser?.FullName ?? "Admin";
                _context.TermWindows.Update(termWindow);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Term {termNumber} dates updated successfully.";
            return RedirectToAction(nameof(Index), new { term = termNumber });
        }
    }
}