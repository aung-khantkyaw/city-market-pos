using CityMarketPOS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public interface ISaleRepository
    {
        Task<IEnumerable<Sale>> GetAllAsync();
        Task<Sale> GetByIdAsync(int id);
        Task AddAsync(Sale sale);
        void Update(Sale sale);
        void Delete(Sale sale);
        Task SaveChangesAsync();
    }
}
