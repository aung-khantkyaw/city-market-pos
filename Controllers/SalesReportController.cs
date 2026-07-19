using CityMarketPOS.Models;
using CityMarketPOS.Models.ViewModels;
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

        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null, string cashierId = null, int? counterId = null)
        {
            if (!startDate.HasValue)
                startDate = DateTime.Today;

            if (!endDate.HasValue)
                endDate = DateTime.Today;

            var actualEndDate = endDate.Value.Date.AddDays(1).AddTicks(-1);

            var query = _context.Sales
                .Include(s => s.Details)
                .Include(s => s.POSSession)
                    .ThenInclude(p => p.Counter)
                .Where(s => s.SaleDate >= startDate.Value.Date && s.SaleDate <= actualEndDate);

            if (!string.IsNullOrEmpty(cashierId))
            {
                query = query.Where(s => s.CashierId == cashierId);
            }

            if (counterId.HasValue)
            {
                query = query.Where(s => s.CounterId == counterId.Value);
            }

            var sales = await query.OrderByDescending(s => s.SaleDate).ToListAsync();

            var totalSales = sales.Count;
            var totalAmount = sales.Sum(s => s.GrandTotal);
            var totalItems = sales.Sum(s => s.Details.Sum(d => d.Quantity));
            var totalTax = sales.Sum(s => s.Tax);
            var totalDiscount = sales.Sum(s => s.Discount);

            var cashiers = await _context.Sales
                .Where(s => s.SaleDate >= startDate.Value.Date && s.SaleDate <= actualEndDate)
                .Select(s => new { s.CashierId, s.CashierName })
                .Distinct()
                .ToListAsync();

            var counters = await _context.Counters
                .Where(c => c.Status == "Active")
                .ToListAsync();

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.CashierId = cashierId;
            ViewBag.CounterId = counterId;
            ViewBag.Sales = sales;
            ViewBag.TotalSales = totalSales;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalTax = totalTax;
            ViewBag.TotalDiscount = totalDiscount;
            ViewBag.Cashiers = cashiers;
            ViewBag.Counters = counters;

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

            ViewBag.RecentAdjustments = recentAdjustments;

            return View();
        }

        public async Task<IActionResult> SessionReport(int sessionId)
        {
            var session = await _context.POSSessions
                .Include(s => s.Counter)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                return NotFound();
            }

            var sales = await _context.Sales
                .Where(s => s.POSSessionId == sessionId)
                .Include(s => s.Details)
                .ToListAsync();

            var viewModel = new SessionReportViewModel
            {
                Session = session,
                Sales = sales,
                TotalSales = sales.Count,
                TotalAmount = sales.Sum(s => s.GrandTotal),
                TotalItems = sales.Sum(s => s.Details.Sum(d => d.Quantity))
            };

            return View(viewModel);
        }
    }
}
