namespace CityMarketPOS.Models.ViewModels
{
    public class LowStockViewModel
    {
        public string ProductName { get; set; }
        public int TotalCurrentStoreQuantity { get; set; }
        public int MinStockLevel { get; set; }
    }
}
