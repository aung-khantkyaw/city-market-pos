using System.ComponentModel.DataAnnotations;

namespace CityMarketPOS.Models
{
    public class UOM
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; } // e.g., Kilogram, Piece, Box

        [Required, StringLength(10)]
        public string ShortName { get; set; } // e.g., KG, PCS, BOX
    }
}