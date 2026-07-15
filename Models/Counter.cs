using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityMarketPOS.Models
{
    public class Counter
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }  // e.g., "Counter 1", "Front Desk"
        
        [StringLength(100)]
        public string? Location { get; set; }  // e.g., "Ground Floor", "Near Entrance"
        
        [Required]
        public string Status { get; set; } = "Active";  // "Active", "Inactive", "Maintenance"
        
        [StringLength(100)]
        public string? AssignedUserId { get; set; }  // User assigned to this counter
        
        [StringLength(100)]
        public string? AssignedUserName { get; set; }
        
        [StringLength(255)]
        public string? Description { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        [StringLength(100)]
        public string? CreatedByUserId { get; set; }
        
        [StringLength(100)]
        public string? CreatedByUserName { get; set; }
        
        public DateTime? ModifiedDate { get; set; }
        
        [StringLength(100)]
        public string? ModifiedByUserId { get; set; }
        
        [StringLength(100)]
        public string? ModifiedByUserName { get; set; }
    }
}
