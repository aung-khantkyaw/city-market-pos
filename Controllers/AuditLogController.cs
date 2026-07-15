using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager")]
    public class AuditLogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditLogController> _logger;

        public AuditLogController(ApplicationDbContext context, ILogger<AuditLogController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string entityType = "", string auditAction = "", string userId = "", string search = "", int page = 1)
        {
            // Clear empty string parameters to prevent them from being treated as filters
            entityType = entityType == "" ? null : entityType;
            auditAction = auditAction == "" ? null : auditAction;
            userId = userId == "" ? null : userId;
            search = search == "" ? null : search;

            var query = _context.AuditLogs.AsQueryable();

            _logger.LogInformation($"Index called with parameters - entityType: '{entityType}', auditAction: '{auditAction}', userId: '{userId}', search: '{search}'");

            // Filter by entity type
            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
                _logger.LogInformation($"Applied entityType filter: {entityType}");
            }

            // Filter by action
            if (!string.IsNullOrWhiteSpace(auditAction))
            {
                query = query.Where(a => a.Action == auditAction);
                _logger.LogInformation($"Applied action filter: {auditAction}");
            }

            // Filter by user ID
            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(a => a.UserId == userId);
                _logger.LogInformation($"Applied userId filter: {userId}");
            }

            // Search in description
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a => a.Description.Contains(search) || a.UserName.Contains(search));
                _logger.LogInformation($"Applied search filter: {search}");
            }

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            // Get unique values for filters
            try
            {
                ViewBag.EntityTypes = await _context.AuditLogs
                    .Select(a => a.EntityType)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToListAsync();

                ViewBag.Actions = await _context.AuditLogs
                    .Select(a => a.Action)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToListAsync();

                ViewBag.Users = await _context.AuditLogs
                    .Select(a => new { a.UserId, a.UserName })
                    .Distinct()
                    .OrderBy(u => u.UserName)
                    .ToListAsync();
                // Debug: Log the count
                _logger.LogInformation($"1AuditLogController: Returning {logs.Count} logs");
                _logger.LogInformation($"1Filters - EntityType: '{entityType}', Action: '{auditAction}', UserId: '{userId}', Search: '{search}'");

            }
            catch
            {
                ViewBag.EntityTypes = new List<string>();
                ViewBag.Actions = new List<string>();
                ViewBag.Users = new List<object>();
                // Debug: Log the count
                _logger.LogInformation($"2AuditLogController: Returning {logs.Count} logs");
                _logger.LogInformation($"2Filters - EntityType: '{entityType}', Action: '{auditAction}', UserId: '{userId}', Search: '{search}'");
            }

            ViewBag.SelectedEntityType = entityType;
            ViewBag.SelectedAction = auditAction;
            ViewBag.SelectedUserId = userId;
            ViewBag.Search = search;
            
            // Debug: Log the count
            _logger.LogInformation($"3AuditLogController: Returning {logs.Count} logs");
            _logger.LogInformation($"3Filters - EntityType: '{entityType}', Action: '{auditAction}', UserId: '{userId}', Search: '{search}'");

            return View(logs);
        }

        public async Task<IActionResult> Details(int id)
        {
            var log = await _context.AuditLogs.FindAsync(id);
            if (log == null) return NotFound();
            return View(log);
        }
    }
}
