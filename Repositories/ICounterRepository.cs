using CityMarketPOS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public interface ICounterRepository
    {
        Task<IEnumerable<Counter>> GetAllAsync();
        Task<Counter> GetByIdAsync(int id);
        Task AddAsync(Counter counter);
        void Update(Counter counter);
        void Delete(Counter counter);
        Task SaveChangesAsync();
    }
}
