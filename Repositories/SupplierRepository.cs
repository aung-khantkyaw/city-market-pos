using CityMarketPOS.Models;
using Microsoft.EntityFrameworkCore;

namespace CityMarketPOS.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly ApplicationDbContext _context;

        public SupplierRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Supplier>> GetAllAsync() => await _context.Suppliers.ToListAsync();

        public async Task<Supplier> GetByIdAsync(int id) => await _context.Suppliers.FindAsync(id);

        public async Task AddAsync(Supplier supplier) => await _context.Suppliers.AddAsync(supplier);

        public void Update(Supplier supplier) => _context.Suppliers.Update(supplier);

        public void Delete(Supplier supplier) => _context.Suppliers.Remove(supplier);

        public async Task<bool> SaveChangesAsync() => (await _context.SaveChangesAsync()) > 0;
    }
}