using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityMarketPOS.Models
{
    public class POSSession
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string UserId { get; set; }
        
        [StringLength(100)]
        public string? UserName { get; set; }
        
        [Required]
        public int CounterId { get; set; }
        
        [ForeignKey("CounterId")]
        public Counter Counter { get; set; }
        
        [Required]
        public DateTime StartTime { get; set; } = DateTime.Now;
        
        public DateTime? EndTime { get; set; }
        
        [Required]
        public string Status { get; set; } = "Active";  // "Active", "Closed"
        
        [StringLength(255)]
        public string? Notes { get; set; }
        
        public decimal OpeningBalance { get; set; } = 0;
        
        public decimal ClosingBalance { get; set; } = 0;
    }
}
