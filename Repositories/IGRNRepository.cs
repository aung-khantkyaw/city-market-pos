using CityMarketPOS.Models;

namespace CityMarketPOS.Repositories
{
    public interface IGRNRepository
    {
        Task<IEnumerable<GRN>> GetAllAsync();
        Task<GRN> GetByIdAsync(int id);
        Task<GRN> GetByPurchaseOrderIdAsync(int poId);
        Task<PurchaseOrder> GetPurchaseOrderWithDetailsAsync(int poId);
        Task ConfirmGRNAndUpdateStockAsync(GRN grn, List<int> productIds, List<int> receivedQtys, List<DateTime?> expiryDates, PurchaseOrder po);
        Task<IEnumerable<Product>> GetLowStockProductsAsync();
        Task<IEnumerable<GRNDetail>> GetExpiringItemsAsync(int daysThreshold);
    }
}