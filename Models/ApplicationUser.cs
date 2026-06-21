using Microsoft.AspNetCore.Identity;

namespace CityMarketPOS.Models
{
    public class ApplicationUser : IdentityUser
    {
        public required string FullName { get; set; }
    }
}
