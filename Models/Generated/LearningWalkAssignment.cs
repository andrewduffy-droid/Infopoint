using System;
using System.Collections.Generic;

namespace InfoPoint.Models.Generated;

public partial class LearningWalkAssignment
{
    public int Id { get; set; }

    public DateTime AssignedDate { get; set; }

    public int IsActive { get; set; }

    public string ObserverId { get; set; } = null!;

    public string StaffId { get; set; } = null!;

    public int Term { get; set; }

    public virtual ICollection<LearningWalkFeedback> LearningWalkFeedbacks { get; set; } = new List<LearningWalkFeedback>();

    public virtual AspNetUser Observer { get; set; } = null!;

    public virtual AspNetUser Staff { get; set; } = null!;
}
