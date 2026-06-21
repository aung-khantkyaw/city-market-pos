using CityMarketPOS.Models;

namespace CityMarketPOS.Repositories
{
    public interface IBrandRepository
    {
        Task<IEnumerable<Brand>> GetAllAsync();
        Task<Brand> GetByIdAsync(int id);
        Task AddAsync(Brand brand);
        void Update(Brand brand);
        void Delete(Brand brand);
        Task<bool> SaveChangesAsync();
    }
}