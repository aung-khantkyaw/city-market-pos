using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityMarketPOS.Models
{
    public class SaleDetail
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int SaleId { get; set; }
        
        [ForeignKey("SaleId")]
        public Sale Sale { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [ForeignKey("ProductId")]
        public Product Product { get; set; }
        
        [Required]
        public int GRNDetailId { get; set; }
        
        [ForeignKey("GRNDetailId")]
        public GRNDetail GRNDetail { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        [Required]
        public decimal UnitPrice { get; set; }
        
        [Required]
        public decimal TotalPrice { get; set; }
        
        [Required]
        public decimal Discount { get; set; } = 0;
        
        [Required]
        public decimal LineTotal { get; set; }  // TotalPrice - Discount
    }
}
