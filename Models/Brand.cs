using System.ComponentModel.DataAnnotations;

namespace CityMarketPOS.Models
{
    public class Brand
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }
    }
}