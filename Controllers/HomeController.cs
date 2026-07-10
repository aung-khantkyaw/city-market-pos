using CityMarketPOS.Models.ViewModels;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CityMarketPOS.Controllers
{
    public class HomeController : Controller
    {
        //public IActionResult Index()
        //{
        //    return View();
        //}

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

        public HomeController(IGRNRepository grnRepo, ApplicationDbContext context)
        {
            _grnRepo = grnRepo;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var lowStockItems = await _context.Products
                .Select(p => new LowStockViewModel
                {
                    ProductName = p.Name,
                    MinStockLevel = p.MinStockLevel,
                    TotalCurrentStockQuantity = _context.GRNDetails
                                                .Where(g => g.ProductId == p.Id)
                                                .Sum(g => (int?)g.CurrentStockQuantity) ?? 0
                })
                .Where(x => x.TotalCurrentStockQuantity <= x.MinStockLevel) 
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

            return View();
        }
    }
}
