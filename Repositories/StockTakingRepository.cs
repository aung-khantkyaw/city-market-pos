using CityMarketPOS.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public class StockTakingRepository : IStockTakingRepository
    {
        private readonly ApplicationDbContext _context;

        public StockTakingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StockTaking>> GetAllAsync()
        {
            return await _context.StockTakings
                .Include(s => s.Details)
                    .ThenInclude(d => d.GRNDetail)
                        .ThenInclude(g => g.Product)
                .OrderByDescending(s => s.TakingDate)
                .ToListAsync();
        }

        public async Task<StockTaking> GetByIdAsync(int id)
        {
            return await _context.StockTakings
                .Include(s => s.Details)
                    .ThenInclude(d => d.GRNDetail)
                        .ThenInclude(g => g.Product)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task AddAsync(StockTaking stockTaking)
        {
            _context.StockTakings.Add(stockTaking);
            await _context.SaveChangesAsync();
        }

        public void Update(StockTaking stockTaking)
        {
            _context.StockTakings.Update(stockTaking);
        }

        public void Delete(StockTaking stockTaking)
        {
            _context.StockTakings.Remove(stockTaking);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
