using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityMarketPOS.Models
{
    public class StockTransferLog
    {
        [Key]
        public int Id { get; set; }
        public int GRNDetailId { get; set; } 
        [ForeignKey("GRNDetailId")]
        public GRNDetail GRNDetail { get; set; }

        public int TransferQuantity { get; set; } 
        public DateTime TransferDate { get; set; } 
        public string? Remarks { get; set; } 
    }
}
