using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityMarketPOS.Models
{
    public class StockAdjustment
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int GRNDetailId { get; set; }
        
        [ForeignKey("GRNDetailId")]
        public GRNDetail GRNDetail { get; set; }
        
        [Required]
        [StringLength(20)]
        public string AdjustmentType { get; set; }  // "In", "Out", "Damage", "Loss", "Theft", "Correction"
        
        [Required]
        public int Quantity { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Reason { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        [Required]
        public string AdjustedByUserId { get; set; }
        
        [StringLength(100)]
        public string? AdjustedByUserName { get; set; }
        
        public DateTime AdjustmentDate { get; set; } = DateTime.Now;
        
        [StringLength(50)]
        public string? IpAddress { get; set; }
    }
}
