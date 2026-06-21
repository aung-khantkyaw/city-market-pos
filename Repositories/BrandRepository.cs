using CityMarketPOS.Models;
using Microsoft.EntityFrameworkCore;

namespace CityMarketPOS.Repositories
{
    public class BrandRepository : IBrandRepository
    {
        private readonly ApplicationDbContext _context;

        public BrandRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Brand>> GetAllAsync() => await _context.Brands.ToListAsync();

        public async Task<Brand> GetByIdAsync(int id) => await _context.Brands.FindAsync(id);

        public async Task AddAsync(Brand brand) => await _context.Brands.AddAsync(brand);

        public void Update(Brand brand) => _context.Brands.Update(brand);

        public void Delete(Brand brand) => _context.Brands.Remove(brand);

        public async Task<bool> SaveChangesAsync() => (await _context.SaveChangesAsync()) > 0;
    }
}