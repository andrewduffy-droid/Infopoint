using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InfoPoint.Models
{
    [Table("bc_v_full_timetable_2025")]
    public class TimetableEntry
    {
        [Key]
        [Column("Event ID")]
        public int EventId { get; set; }
        
        [Column("Event Name")]
        public string? EventName { get; set; }
        
        [Column("StaffReference")]
        public string? StaffReference { get; set; }
        
        [Column("StaffName")]
        public string? StaffName { get; set; }
        
        public string? Room { get; set; }
        
        [Column("weekdayname")]
        public string? WeekdayName { get; set; }
        
        [Column("ClassId")]
        public string? ClassId { get; set; }
        
        // Date/time fields - ignored in DB context due to casting issues, use defaults
        public string? StartTime { get; set; } = "TBC";
        public string? EndTime { get; set; } = "TBC";
        public string? EventDate { get; set; } = "TBC";
        
        // Helper property for date filtering - show as future for now
        [NotMapped]
        public DateTime? EventDateTime => DateTime.Now.AddDays(1);
        
        // Helper property for display
        [NotMapped]
        public string DisplayText => $"{WeekdayName} {EventDate} | {StartTime}-{EndTime} | {Room} | {EventName}";
        
        [NotMapped]
        public string LessonDetails => $"{EventName} - {WeekdayName} {EventDate} from {StartTime} to {EndTime} in {Room}";
    }
}