using Microsoft.AspNetCore.Identity;

namespace InfoPoint.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? GoogleId { get; set; }
        public int? StaffReference { get; set; } // To match with timetable data
        public bool IsObserver { get; set; } = false;
        public DateTime DateCreated { get; set; }
        public DateTime LastLoginDate { get; set; }
    }
}