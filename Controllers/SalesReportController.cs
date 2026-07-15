using CityMarketPOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager")]
    public class SalesReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null)
        {
            // Default to last 30 days if no dates provided
            if (!startDate.HasValue)
                startDate = DateTime.Now.AddDays(-30);
            if (!endDate.HasValue)
                endDate = DateTime.Now;

            // Get inventory value metrics (as proxy for sales data since no sales model exists)
            var totalStockValue = await _context.GRNDetails
                .Where(g => g.CurrentStockQuantity > 0)
                .SumAsync(g => g.CurrentStockQuantity * g.PurchasePrice);

            var totalStockSellingValue = await _context.GRNDetails
                .Where(g => g.CurrentStockQuantity > 0)
                .SumAsync(g => g.CurrentStockQuantity * g.SellingPrice);

            var totalProducts = await _context.Products.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();
            var totalSuppliers = await _context.Suppliers.CountAsync();

            var lowStockItems = await _context.GRNDetails
                .Include(g => g.Product)
                .Where(g => g.CurrentStockQuantity <= g.Product.MinStockLevel)
                .CountAsync();

            var expiringItems = await _context.GRNDetails
                .Where(g => g.ExpiryDate.HasValue && 
                            g.ExpiryDate <= DateTime.Now.AddDays(30) && 
                            g.CurrentStockQuantity > 0)
                .CountAsync();

            // Category-wise stock distribution
            var categoryDistribution = await _context.GRNDetails
                .Include(g => g.Product)
                    .ThenInclude(p => p.Category)
                .Where(g => g.CurrentStockQuantity > 0)
                .GroupBy(g => g.Product.Category.Name ?? "Uncategorized")
                .Select(g => new
                {
                    Category = g.Key,
                    TotalValue = g.Sum(x => x.CurrentStockQuantity * x.PurchasePrice),
                    TotalQuantity = g.Sum(x => x.CurrentStockQuantity)
                })
                .ToListAsync();

            // Stock value by category
            var stockByCategory = await _context.GRNDetails
                .Include(g => g.Product)
                    .ThenInclude(p => p.Category)
                .Where(g => g.CurrentStockQuantity > 0)
                .GroupBy(g => g.Product.Category)
                .Select(g => new
                {
                    CategoryName = g.Key.Name,
                    TotalStock = g.Sum(x => x.CurrentStockQuantity),
                    TotalValue = g.Sum(x => x.CurrentStockQuantity * x.PurchasePrice),
                    PotentialRevenue = g.Sum(x => x.CurrentStockQuantity * x.SellingPrice)
                })
                .OrderByDescending(x => x.TotalValue)
                .ToListAsync();

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.TotalStockValue = totalStockValue;
            ViewBag.TotalStockSellingValue = totalStockSellingValue;
            ViewBag.PotentialProfit = totalStockSellingValue - totalStockValue;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalCategories = totalCategories;
            ViewBag.TotalSuppliers = totalSuppliers;
            ViewBag.LowStockItems = lowStockItems;
            ViewBag.ExpiringItems = expiringItems;
            ViewBag.CategoryDistribution = categoryDistribution;
            ViewBag.StockByCategory = stockByCategory;

            return View();
        }

        public async Task<IActionResult> InventoryValuation()
        {
            var stockByCategory = await _context.GRNDetails
                .Include(g => g.Product)
                    .ThenInclude(p => p.Category)
                .Where(g => g.CurrentStockQuantity > 0)
                .GroupBy(g => g.Product.Category)
                .Select(g => new
                {
                    CategoryName = g.Key.Name,
                    TotalStock = g.Sum(x => x.CurrentStockQuantity),
                    TotalValue = g.Sum(x => x.CurrentStockQuantity * x.PurchasePrice),
                    PotentialRevenue = g.Sum(x => x.CurrentStockQuantity * x.SellingPrice),
                    ProfitMargin = g.Sum(x => x.CurrentStockQuantity * (x.SellingPrice - x.PurchasePrice))
                })
                .OrderByDescending(x => x.TotalValue)
                .ToListAsync();

            var totalValue = stockByCategory.Sum(x => x.TotalValue);
            var totalRevenue = stockByCategory.Sum(x => x.PotentialRevenue);
            var totalProfit = stockByCategory.Sum(x => x.ProfitMargin);

            ViewBag.StockByCategory = stockByCategory;
            ViewBag.TotalValue = totalValue;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalProfit = totalProfit;

            return View();
        }

        public async Task<IActionResult> StockMovement()
        {
            // Get recent stock adjustments
            var recentAdjustments = await _context.StockAdjustments
                .Include(s => s.GRNDetail)
                    .ThenInclude(g => g.Product)
                .OrderByDescending(s => s.AdjustmentDate)
                .Take(50)
                .ToListAsync();

            // Get recent stock takings
            var recentStockTakings = await _context.StockTakings
                .Include(s => s.Details)
                    .ThenInclude(d => d.GRNDetail)
                        .ThenInclude(g => g.Product)
                .OrderByDescending(s => s.TakingDate)
                .Take(20)
                .ToListAsync();

            ViewBag.RecentAdjustments = recentAdjustments;
            ViewBag.RecentStockTakings = recentStockTakings;

            return View();
        }
    }
}
