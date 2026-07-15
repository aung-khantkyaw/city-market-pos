using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager,Inventory")]
    public class ExpiryManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExpiryManagementController(ApplicationDbContext context, IAuditLogRepository auditLogRepo, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditLogRepo = auditLogRepo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int daysThreshold = 30)
        {
            var thresholdDate = DateTime.Now.AddDays(daysThreshold);
            
            var expiringItems = await _context.GRNDetails
                .Include(g => g.Product)
                    .ThenInclude(p => p.Category)
                .Include(g => g.GRN)
                .Where(g => g.ExpiryDate.HasValue && 
                            g.ExpiryDate <= thresholdDate && 
                            g.CurrentStockQuantity > 0)
                .OrderBy(g => g.ExpiryDate)
                .ToListAsync();

            ViewBag.DaysThreshold = daysThreshold;
            ViewBag.ExpiringCount = expiringItems.Count;
            ViewBag.ExpiredCount = expiringItems.Count(g => g.ExpiryDate < DateTime.Now);
            ViewBag.ExpiringSoonCount = expiringItems.Count(g => g.ExpiryDate >= DateTime.Now);

            return View(expiringItems);
        }

        public async Task<IActionResult> WriteOff(int id, string reason)
        {
            var grnDetail = await _context.GRNDetails
                .Include(g => g.Product)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grnDetail == null) return NotFound();

            var previousQuantity = grnDetail.CurrentStockQuantity;
            grnDetail.CurrentStockQuantity = 0;
            _context.GRNDetails.Update(grnDetail);
            await _context.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);
            await _auditLogRepo.LogAsync("ExpiryManagement", id.ToString(), "WriteOff", user?.Id ?? "System", user?.UserName ?? "System", $"Wrote off expired item: {grnDetail.Product.Name} (Qty: {previousQuantity}) - Reason: {reason}");

            TempData["Success"] = "Item written off successfully!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Discount(int id, decimal discountPercentage, string reason)
        {
            var grnDetail = await _context.GRNDetails
                .Include(g => g.Product)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grnDetail == null) return NotFound();

            var originalPrice = grnDetail.SellingPrice;
            grnDetail.SellingPrice = originalPrice * (1 - discountPercentage / 100);
            _context.GRNDetails.Update(grnDetail);
            await _context.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);
            await _auditLogRepo.LogAsync("ExpiryManagement", id.ToString(), "Discount", user?.Id ?? "System", user?.UserName ?? "System", $"Applied {discountPercentage}% discount to {grnDetail.Product.Name} - Original: {originalPrice:F2}, New: {grnDetail.SellingPrice:F2} - Reason: {reason}");

            TempData["Success"] = "Discount applied successfully!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var grnDetail = await _context.GRNDetails
                .Include(g => g.Product)
                    .ThenInclude(p => p.Category)
                .Include(g => g.GRN)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grnDetail == null) return NotFound();
            return View(grnDetail);
        }
    }
}
