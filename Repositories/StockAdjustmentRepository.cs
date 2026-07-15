using CityMarketPOS.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public class StockAdjustmentRepository : IStockAdjustmentRepository
    {
        private readonly ApplicationDbContext _context;

        public StockAdjustmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StockAdjustment>> GetAllAsync()
        {
            return await _context.StockAdjustments
                .Include(s => s.GRNDetail)
                    .ThenInclude(g => g.Product)
                .OrderByDescending(s => s.AdjustmentDate)
                .ToListAsync();
        }

        public async Task<StockAdjustment> GetByIdAsync(int id)
        {
            return await _context.StockAdjustments
                .Include(s => s.GRNDetail)
                    .ThenInclude(g => g.Product)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task AddAsync(StockAdjustment adjustment)
        {
            _context.StockAdjustments.Add(adjustment);
            await _context.SaveChangesAsync();
        }

        public void Update(StockAdjustment adjustment)
        {
            _context.StockAdjustments.Update(adjustment);
        }

        public void Delete(StockAdjustment adjustment)
        {
            _context.StockAdjustments.Remove(adjustment);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
