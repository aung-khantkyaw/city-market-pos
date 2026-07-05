using CityMarketPOS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public interface IGRNRepository
    {
        Task<IEnumerable<GRN>> GetAllAsync();
        Task<GRN> GetByIdAsync(int id);
        Task<IEnumerable<GRN>> GetByPurchaseOrderIdAsync(int poId);

        Task<PurchaseOrder> GetPurchaseOrderWithDetailsAsync(int poId);
        Task ConfirmGRNAndUpdateStockAsync(GRN grn, List<int> productIds, List<int> receivedQtys, List<DateTime?> expiryDates, List<decimal> sellingPrices, List<string> codeTypes, List<string> codePrefixes, List<string> codeValues, PurchaseOrder po);
        Task<IEnumerable<Product>> GetLowStockProductsAsync();
        Task<IEnumerable<GRNDetail>> GetExpiringItemsAsync(int daysThreshold);
        Task<Dictionary<int, int>> GetReceivedQuantitiesByPOAsync(int poId);
    }
}