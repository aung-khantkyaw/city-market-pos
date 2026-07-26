using CityMarketPOS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public interface IGroundBalanceRepository
    {
        Task<IEnumerable<GroundBalance>> GetAllAsync();
        Task<GroundBalance> GetByBatchNoAsync(string batchNo);
        Task AddAsync(GroundBalance groundBalance);
        Task UpdateAsync(GroundBalance groundBalance);
        Task DeleteAsync(string batchNo);
        Task SaveChangesAsync();
    }
}
