using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityMarketPOS.Models
{
    public class PurchaseOrder
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string PONumber { get; set; } // e.g., PO-202606-001

        public int SupplierId { get; set; }
        [ForeignKey("SupplierId")]
        public Supplier Supplier { get; set; }

        public DateTime OrderDate { get; set; }
        public DateTime ExpectedDate { get; set; }

        [Required]
        public string Status { get; set; } // "Pending", "Received", "Cancelled"

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public List<PurchaseOrderDetail> OrderDetails { get; set; }
    }
}