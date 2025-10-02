using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InfoPoint.Data;
using InfoPoint.Models;
using System.Linq;

namespace InfoPoint.Services
{
    public class StaffSyncService
    {
        private readonly ApplicationDbContext _context;
        private readonly TimetableDbContext _timetableContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<StaffSyncService> _logger;

        public StaffSyncService(
            ApplicationDbContext context,
            TimetableDbContext timetableContext,
            UserManager<ApplicationUser> userManager,
            ILogger<StaffSyncService> logger)
        {
            _context = context;
            _timetableContext = timetableContext;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<SyncResult> SyncStaffFromTimetableAsync()
        {
            var result = new SyncResult();
            
            try
            {
                _logger.LogInformation("Starting staff sync from timetable database");

                // Get distinct staff from timetable - filter out invalid references
                // Use a more efficient query that limits the result set
                var timetableStaff = await _timetableContext.TimetableEntries
                    .Where(t => !string.IsNullOrEmpty(t.StaffReference) && !string.IsNullOrEmpty(t.StaffName))
                    .Select(t => new { t.StaffReference, t.StaffName })
                    .Distinct()
                    .Take(1000) // Limit to first 1000 distinct staff members to avoid timeout
                    .ToListAsync();

                // Filter to only numeric staff references and convert to int
                var validStaff = timetableStaff
                    .Where(s => int.TryParse(s.StaffReference, out _))
                    .Select(s => new { 
                        StaffReference = int.Parse(s.StaffReference), 
                        StaffName = s.StaffName 
                    })
                    .Where(s => s.StaffReference > 0)
                    .ToList();

                _logger.LogInformation($"Found {timetableStaff.Count} total staff members in timetable");
                _logger.LogInformation($"Found {validStaff.Count} staff members with valid numeric references");
                
                // Log each valid staff member found
                foreach (var staff in validStaff)
                {
                    _logger.LogInformation($"Staff found: {staff.StaffReference} - {staff.StaffName}");
                }

                foreach (var staff in validStaff)
                {
                    // Check if user already exists with this StaffReference
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.StaffReference == staff.StaffReference);

                    if (existingUser != null)
                    {
                        // Update name if changed
                        if (existingUser.FullName != staff.StaffName)
                        {
                            existingUser.FullName = staff.StaffName;
                            await _userManager.UpdateAsync(existingUser);
                            result.UpdatedCount++;
                            _logger.LogInformation($"Updated name for {staff.StaffReference}: {staff.StaffName}");
                        }
                        else
                        {
                            result.SkippedCount++;
                        }
                    }
                    else
                    {
                        // Check if user exists by name (for migration purposes)
                        var userByName = await _context.Users
                            .FirstOrDefaultAsync(u => u.FullName == staff.StaffName);

                        if (userByName != null && !userByName.StaffReference.HasValue)
                        {
                            // Update existing user with StaffReference
                            userByName.StaffReference = staff.StaffReference;
                            await _userManager.UpdateAsync(userByName);
                            result.UpdatedCount++;
                            _logger.LogInformation($"Added StaffReference {staff.StaffReference} to existing user: {staff.StaffName}");
                        }
                        else
                        {
                            // Create new user
                            var email = GenerateEmail(staff.StaffName, staff.StaffReference);
                            _logger.LogInformation($"Generated email for {staff.StaffName}: {email}");
                            var newUser = new ApplicationUser
                            {
                                UserName = email,
                                Email = email,
                                FullName = staff.StaffName,
                                StaffReference = staff.StaffReference,
                                EmailConfirmed = true,
                                DateCreated = DateTime.UtcNow,
                                LastLoginDate = DateTime.UtcNow
                            };

                            var createResult = await _userManager.CreateAsync(newUser);
                            if (createResult.Succeeded)
                            {
                                result.CreatedCount++;
                                _logger.LogInformation($"Created new user: {staff.StaffName} ({staff.StaffReference})");
                            }
                            else
                            {
                                result.Errors.Add($"Failed to create {staff.StaffName}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                                _logger.LogError($"Failed to create user {staff.StaffName}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Sync failed: {ex.Message}");
                _logger.LogError(ex, "Staff sync failed");
            }

            return result;
        }

        private string GenerateEmail(string staffName, int staffReference)
        {
            // Generate a standardized email from staff name
            var nameParts = staffName.Trim().ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (nameParts.Length >= 2)
            {
                // Clean the name parts to remove special characters
                var firstName = CleanNameForEmail(nameParts[0]);
                var lastName = CleanNameForEmail(nameParts[nameParts.Length - 1]);
                
                // firstname.lastname@g.bdc.ac.uk
                return $"{firstName}.{lastName}@g.bdc.ac.uk";
            }
            else if (nameParts.Length == 1)
            {
                // Clean single name and use with staff reference
                var cleanName = CleanNameForEmail(nameParts[0]);
                return $"{cleanName}.{staffReference}@g.bdc.ac.uk";
            }
            else
            {
                // fallback
                return $"staff.{staffReference}@g.bdc.ac.uk";
            }
        }

        private string CleanNameForEmail(string namePart)
        {
            // Remove or replace special characters that aren't allowed in email usernames
            // Keep only letters, digits, and convert some common special characters
            var cleaned = namePart
                .Replace("'", "") // Remove apostrophes (O'Connor -> OConnor)
                .Replace("-", "") // Remove hyphens (Mary-Jane -> MaryJane)
                .Replace(".", "") // Remove periods
                .Replace("ñ", "n") // Replace accented characters
                .Replace("é", "e")
                .Replace("á", "a")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u");
            
            // Remove any remaining non-alphanumeric characters
            return new string(cleaned.Where(c => char.IsLetterOrDigit(c)).ToArray());
        }
    }

    public class SyncResult
    {
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        
        public bool Success => !Errors.Any();
        public string Summary => $"Created: {CreatedCount}, Updated: {UpdatedCount}, Skipped: {SkippedCount}";
    }
}