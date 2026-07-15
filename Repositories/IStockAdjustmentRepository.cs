using CityMarketPOS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public interface IStockAdjustmentRepository
    {
        Task<IEnumerable<StockAdjustment>> GetAllAsync();
        Task<StockAdjustment> GetByIdAsync(int id);
        Task AddAsync(StockAdjustment adjustment);
        void Update(StockAdjustment adjustment);
        void Delete(StockAdjustment adjustment);
        Task SaveChangesAsync();
    }
}
