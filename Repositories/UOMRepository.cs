using CityMarketPOS.Models;
using Microsoft.EntityFrameworkCore;

namespace CityMarketPOS.Repositories
{
    public class UOMRepository : IUOMRepository
    {
        private readonly ApplicationDbContext _context;

        public UOMRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UOM>> GetAllAsync() => await _context.UOMs.ToListAsync();

        public async Task<UOM> GetByIdAsync(int id) => await _context.UOMs.FindAsync(id);

        public async Task AddAsync(UOM uom) => await _context.UOMs.AddAsync(uom);

        public void Update(UOM uom) => _context.UOMs.Update(uom);

        public void Delete(UOM uom) => _context.UOMs.Remove(uom);

        public async Task<bool> SaveChangesAsync() => (await _context.SaveChangesAsync()) > 0;
    }
}