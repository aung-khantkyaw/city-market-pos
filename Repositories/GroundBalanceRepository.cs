using CityMarketPOS.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public class GroundBalanceRepository : IGroundBalanceRepository
    {
        private readonly ApplicationDbContext _context;

        public GroundBalanceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<GroundBalance>> GetAllAsync()
        {
            return await _context.GroundBalances
                .Include(g => g.GroundBalanceDetails)
                .OrderByDescending(g => g.GBDate)
                .ToListAsync();
        }

        public async Task<GroundBalance> GetByBatchNoAsync(string batchNo)
        {
            return await _context.GroundBalances
                .Include(g => g.GroundBalanceDetails)
                .FirstOrDefaultAsync(m => m.BatchNo == batchNo);
        }

        public async Task AddAsync(GroundBalance groundBalance)
        {
            _context.GroundBalances.Add(groundBalance);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(GroundBalance groundBalance)
        {
            _context.GroundBalances.Update(groundBalance);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string batchNo)
        {
            var groundBalance = await _context.GroundBalances.FindAsync(batchNo);
            if (groundBalance != null)
            {
                _context.GroundBalances.Remove(groundBalance);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
