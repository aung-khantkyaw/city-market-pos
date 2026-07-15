using CityMarketPOS.Models;
using CityMarketPOS.Models.ViewModels;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CityMarketPOS.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private readonly IGRNRepository _grnRepo;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(IGRNRepository grnRepo, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _grnRepo = grnRepo;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Common data for all roles
            var lowStockItems = await _context.GRNDetails
                .Include(g => g.Product)
                .Where(g => g.CurrentStockQuantity <= g.Product.MinStockLevel)
                .Select(g => new LowStockViewModel
                {
                    ProductName = g.Product.Name,
                    MinStockLevel = g.Product.MinStockLevel,
                    TotalCurrentStockQuantity = g.CurrentStockQuantity
                })
                .ToListAsync();

            var targetDate = DateTime.Now.AddDays(7);
            var expiringItems = await _context.GRNDetails
                .Include(g => g.Product)
                .Where(g => g.CurrentStockQuantity > 0 && 
                            g.ExpiryDate.HasValue &&      
                            g.ExpiryDate.Value.Date <= targetDate.Date) 
                .OrderBy(g => g.ExpiryDate)
                .ToListAsync();

            ViewBag.LowStock = lowStockItems;
            ViewBag.Expiring = expiringItems;

            // Inventory role specific data
            if (User.IsInRole("Inventory"))
            {
                ViewBag.TotalCategories = await _context.Categories.CountAsync();
                ViewBag.TotalProducts = await _context.Products.CountAsync();
                ViewBag.TotalSuppliers = await _context.Suppliers.CountAsync();
                ViewBag.LowStockCount = lowStockItems.Count;
            }

            // Manager role specific data - full analytics
            if (User.IsInRole("Manager"))
            {
                ViewBag.TotalCategories = await _context.Categories.CountAsync();
                ViewBag.TotalProducts = await _context.Products.CountAsync();
                ViewBag.TotalSuppliers = await _context.Suppliers.CountAsync();
                ViewBag.TotalGRNs = await _context.GRNs.CountAsync();
                ViewBag.TotalPOs = await _context.PurchaseOrders.CountAsync();
                
                // Stock analytics
                ViewBag.TotalStockQuantity = await _context.GRNDetails.SumAsync(g => (int?)g.CurrentStockQuantity) ?? 0;
                ViewBag.LowStockCount = lowStockItems.Count;
                
                // Value analytics
                ViewBag.TotalStockValue = await _context.GRNDetails
                    .Where(g => g.CurrentStockQuantity > 0)
                    .SumAsync(g => g.CurrentStockQuantity * g.PurchasePrice);
                
                // Recent activity
                ViewBag.RecentGRNs = await _context.GRNs
                    .Include(g => g.PurchaseOrder)
                    .OrderByDescending(g => g.ReceivedDate)
                    .Take(5)
                    .ToListAsync();
            }

            // Cashier role specific data - POS session management
            if (User.IsInRole("Cashier"))
            {
                var userId = _userManager.GetUserId(User);
                var activeSession = await _context.POSSessions
                    .Include(s => s.Counter)
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

                ViewBag.ActiveSession = activeSession;

                // Get available counters if no active session
                if (activeSession == null)
                {
                    var availableCounters = await _context.Counters
                        .Where(c => c.Status == "Active")
                        .ToListAsync();
                    ViewBag.Counters = availableCounters;
                }
            }

            return View();
        }
    }
}
