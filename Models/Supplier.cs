using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CityMarketPOS.Models
{
    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; }

        public string ContactPerson { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}