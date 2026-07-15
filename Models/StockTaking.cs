using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityMarketPOS.Models
{
    public class StockTaking
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string StockTakingNumber { get; set; }  // e.g., "ST-20250115-001"
        
        [Required]
        public DateTime TakingDate { get; set; } = DateTime.Now;
        
        [Required]
        public string ConductedByUserId { get; set; }
        
        [StringLength(100)]
        public string? ConductedByUserName { get; set; }
        
        [StringLength(255)]
        public string? Remarks { get; set; }
        
        [Required]
        public string Status { get; set; } = "In Progress";  // "In Progress", "Completed", "Cancelled"
        
        public DateTime? CompletedDate { get; set; }
        
        public List<StockTakingDetail> Details { get; set; } = new List<StockTakingDetail>();
    }

    public class StockTakingDetail
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int StockTakingId { get; set; }
        
        [ForeignKey("StockTakingId")]
        public StockTaking StockTaking { get; set; }
        
        [Required]
        public int GRNDetailId { get; set; }
        
        [ForeignKey("GRNDetailId")]
        public GRNDetail GRNDetail { get; set; }
        
        [Required]
        public int ExpectedQuantity { get; set; }  // System quantity
        
        [Required]
        public int ActualQuantity { get; set; }  // Physical count quantity
        
        [Required]
        public int Variance { get; set; }  // Difference (Actual - Expected)
        
        [StringLength(255)]
        public string? Notes { get; set; }
    }
}
