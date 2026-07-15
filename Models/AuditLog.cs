using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityMarketPOS.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string EntityType { get; set; }  // e.g., "Category", "Product", "Supplier", etc.
        
        [Required]
        public string EntityId { get; set; }  // ID of the affected entity as string
        
        [Required]
        [StringLength(20)]
        public string Action { get; set; }  // e.g., "Create", "Update", "Delete"
        
        [Required]
        public string UserId { get; set; }  // ID of the user who performed the action
        
        [StringLength(100)]
        public string UserName { get; set; }  // Username of the user who performed the action
        
        [StringLength(255)]
        public string Description { get; set; }  // Description of what was changed
        
        public string? OldValues { get; set; }  // JSON string of old values (for updates)
        public string? NewValues { get; set; }  // JSON string of new values
        
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        [StringLength(50)]
        public string? IpAddress { get; set; }  // IP address of the user
    }
}
