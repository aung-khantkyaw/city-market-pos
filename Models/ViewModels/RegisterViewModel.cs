namespace CityMarketPOS.Models.ViewModels
{
    public class RegisterViewModel
    {
        public required string UserName { get; set; }
        public required string Password { get; set; }
        public required string FullName { get; set; }
        public required string Role { get; set; } // Manager, Inventory, Cashier
    }
}
