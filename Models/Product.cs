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

        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public int UOMId { get; set; }
        [ForeignKey("UOMId")]
        public UOM? UOM { get; set; }

        public int MinStockLevel { get; set; } = 5; 

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; } = false;
        public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    }
}