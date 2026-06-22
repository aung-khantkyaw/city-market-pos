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
        public int CurrentQuantity { get; set; } 
        public DateTime? ExpiryDate { get; set; }
    }
}
