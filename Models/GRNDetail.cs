using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityMarketPOS.Models
{
    public class GRNDetail
    {
        [Key]
        public int Id { get; set; }
        public int GRNId { get; set; }
        [ForeignKey("GRNId")]
        public GRN GRN { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }
        public int ReceivedQuantity { get; set; }
        public int StockQuantity { get; set; }
        public int CurrentStockQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; } = 0;

        [StringLength(100)] 
        public string? ItemCode { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}