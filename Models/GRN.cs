using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityMarketPOS.Models
{
    public class GRN
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string GRNNumber { get; set; } // e.g., GRN-202606-001

        public int PurchaseOrderId { get; set; }
        [ForeignKey("PurchaseOrderId")]
        public PurchaseOrder PurchaseOrder { get; set; }

        public DateTime ReceivedDate { get; set; } = DateTime.Now;

        // Optionally link to the User who received it
        public string ReceivedByUserId { get; set; }

        public string Remarks { get; set; }
    }
}