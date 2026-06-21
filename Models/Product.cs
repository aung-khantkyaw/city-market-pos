using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityMarketPOS.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; }

        [Required, StringLength(50)]
        public string Barcode { get; set; } // Can be auto-generated later

        // Foreign Keys
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public int BrandId { get; set; }
        [ForeignKey("BrandId")]
        public Brand? Brand { get; set; }

        public int UOMId { get; set; }
        [ForeignKey("UOMId")]
        public UOM? UOM { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }

        public int StockQuantity { get; set; } // Will be updated by GRN

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}