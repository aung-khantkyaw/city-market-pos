using CityMarketPOS.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public class POSSessionRepository : IPOSSessionRepository
    {
        private readonly ApplicationDbContext _context;

        public POSSessionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<POSSession>> GetAllAsync()
        {
            return await _context.POSSessions
                .Include(s => s.Counter)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<POSSession> GetByIdAsync(int id)
        {
            return await _context.POSSessions
                .Include(s => s.Counter)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<POSSession> GetActiveSessionByUserIdAsync(string userId)
        {
            return await _context.POSSessions
                .Include(s => s.Counter)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");
        }

        public async Task AddAsync(POSSession session)
        {
            _context.POSSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        public void Update(POSSession session)
        {
            _context.POSSessions.Update(session);
        }

        public void Delete(POSSession session)
        {
            _context.POSSessions.Remove(session);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
