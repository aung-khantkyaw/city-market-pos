namespace CityMarketPOS.Models.ViewModels
{
    public class PurchaseOrderViewModel
    {
        public string PONumber { get; set; }
        public int SupplierId { get; set; }
        public List<PurchaseOrderDetailViewModel> OrderDetails { get; set; }
    }
}
