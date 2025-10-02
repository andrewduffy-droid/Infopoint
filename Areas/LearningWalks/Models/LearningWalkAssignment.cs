using System;
using System.ComponentModel.DataAnnotations;
using InfoPoint.Models;

namespace InfoPoint.Areas.LearningWalks.Models
{
    public class LearningWalkAssignment
    {
        public int Id { get; set; }
        
        [Required]
        public string ObserverId { get; set; }
        public ApplicationUser Observer { get; set; }
        
        [Required]
        public string StaffId { get; set; }
        public ApplicationUser Staff { get; set; }
        
        public DateTime AssignedDate { get; set; }
        
        [Required]
        public int Term { get; set; } = 1; // 1, 2, or 3
        
        public bool IsActive { get; set; } = true;
    }
}