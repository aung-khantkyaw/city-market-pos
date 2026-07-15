using CityMarketPOS.Models;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public interface IAuditLogRepository
    {
        Task LogAsync(string entityType, string entityId, string action, string userId, string userName, string description, string? oldValues = null, string? newValues = null, string? ipAddress = null);
    }
}
