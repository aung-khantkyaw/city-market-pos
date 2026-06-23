using System;
using System.Collections.Generic;
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

        //[Required, StringLength(50)]
        //public string Barcode { get; set; }

        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public int UOMId { get; set; }
        [ForeignKey("UOMId")]
        public UOM? UOM { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; } = 0; 

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; } = 0;

        public int StoreQuantity { get; set; } = 0;
        public int StockQuantity { get; set; } = 0;

        [NotMapped]
        public int TotalQuantity => StoreQuantity + StockQuantity;

        public int MinStockLevel { get; set; } = 5; 

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    }
}