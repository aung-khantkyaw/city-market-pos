using CityMarketPOS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public interface IBatchTrackingRepository
    {
        Task<IEnumerable<GRNDetail>> GetActiveBatchesAsync();

        Task<GRNDetail> GetBatchDetailByIdAsync(int detailId);

        Task<(bool Success, string Message)> TransferToStockAsync(int detailId, int qty);
    }
}