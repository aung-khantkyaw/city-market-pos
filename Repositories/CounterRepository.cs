using CityMarketPOS.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public class CounterRepository : ICounterRepository
    {
        private readonly ApplicationDbContext _context;

        public CounterRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Counter>> GetAllAsync()
        {
            return await _context.Counters
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }

        public async Task<Counter> GetByIdAsync(int id)
        {
            return await _context.Counters.FindAsync(id);
        }

        public async Task AddAsync(Counter counter)
        {
            _context.Counters.Add(counter);
            await _context.SaveChangesAsync();
        }

        public void Update(Counter counter)
        {
            _context.Counters.Update(counter);
        }

        public void Delete(Counter counter)
        {
            _context.Counters.Remove(counter);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
