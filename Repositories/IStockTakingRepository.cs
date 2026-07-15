using CityMarketPOS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public interface IStockTakingRepository
    {
        Task<IEnumerable<StockTaking>> GetAllAsync();
        Task<StockTaking> GetByIdAsync(int id);
        Task AddAsync(StockTaking stockTaking);
        void Update(StockTaking stockTaking);
        void Delete(StockTaking stockTaking);
        Task SaveChangesAsync();
    }
}
