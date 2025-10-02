using System;
using System.ComponentModel.DataAnnotations;

namespace InfoPoint.Areas.LearningWalks.Models
{
    public class TermWindow
    {
        public int Id { get; set; }
        
        [Required]
        public int Term { get; set; } // 1, 2, or 3
        
        public bool IsActive { get; set; }
        
        public DateTime? StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        public DateTime LastModified { get; set; }
        
        public string ModifiedBy { get; set; }
    }
}