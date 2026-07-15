using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityMarketPOS.Models
{
    public class Sale
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string SaleNumber { get; set; }  // e.g., "SALE-20250115-001"
        
        [Required]
        public DateTime SaleDate { get; set; } = DateTime.Now;
        
        [Required]
        public int POSSessionId { get; set; }
        
        [ForeignKey("POSSessionId")]
        public POSSession POSSession { get; set; }
        
        [Required]
        [StringLength(100)]
        public string CashierId { get; set; }
        
        [StringLength(100)]
        public string? CashierName { get; set; }
        
        [Required]
        public int CounterId { get; set; }
        
        [ForeignKey("CounterId")]
        public Counter Counter { get; set; }
        
        [Required]
        public decimal Subtotal { get; set; } = 0;
        
        [Required]
        public decimal Tax { get; set; } = 0;  // 5% tax
        
        [Required]
        public decimal Total { get; set; } = 0;
        
        [Required]
        public decimal Discount { get; set; } = 0;
        
        [Required]
        public decimal GrandTotal { get; set; } = 0;  // Total - Discount
        
        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash";  // "Cash", "Card", "Mobile Payment"
        
        [StringLength(100)]
        public string? CardNumber { get; set; }
        
        [Required]
        public string Status { get; set; } = "Pending";  // "Pending", "Completed", "Cancelled", "Refunded"
        
        [StringLength(255)]
        public string? Notes { get; set; }
        
        public List<SaleDetail> Details { get; set; } = new List<SaleDetail>();
    }
}
