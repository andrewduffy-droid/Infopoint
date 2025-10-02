using System;
using System.Collections.Generic;

namespace InfoPoint.Models.Generated;

public partial class TermWindow
{
    public int Id { get; set; }

    public int Term { get; set; }

    public int IsActive { get; set; }

    public string? StartDate { get; set; }

    public string? EndDate { get; set; }

    public DateTime LastModified { get; set; }

    public string ModifiedBy { get; set; } = null!;
}
