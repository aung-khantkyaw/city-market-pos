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
        public string GRNNumber { get; set; }
        public int PurchaseOrderId { get; set; }
        [ForeignKey("PurchaseOrderId")]
        public PurchaseOrder PurchaseOrder { get; set; }
        public DateTime ReceivedDate { get; set; } = DateTime.Now;
        public string ReceivedByUserId { get; set; }
        public string? Remarks { get; set; }
        public List<GRNDetail> GRNDetails { get; set; } = new List<GRNDetail>();
    }
}