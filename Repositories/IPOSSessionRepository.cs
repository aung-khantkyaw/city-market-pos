using CityMarketPOS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public interface IPOSSessionRepository
    {
        Task<IEnumerable<POSSession>> GetAllAsync();
        Task<POSSession> GetByIdAsync(int id);
        Task<POSSession> GetActiveSessionByUserIdAsync(string userId);
        Task AddAsync(POSSession session);
        void Update(POSSession session);
        void Delete(POSSession session);
        Task SaveChangesAsync();
    }
}
