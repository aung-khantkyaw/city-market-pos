using CityMarketPOS.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ApplicationDbContext _context;

        public AuditLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string entityType, string entityId, string action, string userId, string userName, string description, string? oldValues = null, string? newValues = null, string? ipAddress = null)
        {
            var auditLog = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                UserName = userName,
                Description = description,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = ipAddress
            };
            Console.WriteLine("Audit LogAsync: " + auditLog);
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}
