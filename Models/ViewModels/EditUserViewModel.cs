using System.ComponentModel.DataAnnotations;

namespace CityMarketPOS.Models.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Role { get; set; }
    }
}
