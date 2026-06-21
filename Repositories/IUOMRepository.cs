using CityMarketPOS.Models;

namespace CityMarketPOS.Repositories
{
    public interface IUOMRepository
    {
        Task<IEnumerable<UOM>> GetAllAsync();
        Task<UOM> GetByIdAsync(int id);
        Task AddAsync(UOM uom);
        void Update(UOM uom);
        void Delete(UOM uom);
        Task<bool> SaveChangesAsync();
    }
}