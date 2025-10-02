using System;
using System.Collections.Generic;

namespace InfoPoint.Models.Generated;

public partial class BcVFullTimetable2025
{
    public int EventId { get; set; }

    public string? Directorate { get; set; }

    public string? QualCurriculumAreaName { get; set; }

    public string? ProgReference { get; set; }

    public string? ProgName { get; set; }

    public string? Site { get; set; }

    public string? Room { get; set; }

    public string? RoomBlock { get; set; }

    public string? EventName { get; set; }

    public int StaffReference { get; set; }

    public string? StaffName { get; set; }

    public int Hours { get; set; }

    public string? StartTime { get; set; }

    public string? EndTime { get; set; }

    public string? Weekdayname { get; set; }

    public int WeekdayNo { get; set; }

    public string Eventdate { get; set; } = null!;

    public int WeekNumber { get; set; }

    public int ClassId { get; set; }

    public string StartDate { get; set; } = null!;

    public string EndDate { get; set; } = null!;

    public int MinuteDuration { get; set; }

    public string DateRun { get; set; } = null!;
}
