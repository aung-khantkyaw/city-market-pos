using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace CityMarketPOS.Models
{
    [Index(nameof(ShortName), IsUnique = true)]
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, StringLength(10)]
        public string ShortName { get; set; }

        public string Description { get; set; }
    }
}