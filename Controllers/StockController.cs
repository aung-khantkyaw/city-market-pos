using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager,Inventory")]
    public class StockController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IGRNRepository _grnRepo;

        public StockController(ApplicationDbContext context, IGRNRepository grnRepo)
        {
            _context = context;
            _grnRepo = grnRepo;
        }

        public async Task<IActionResult> Index(string search = "", int? categoryId = null, bool lowStock = false, bool expiringSoon = false)
        {
            var query = _context.GRNDetails
                .Include(g => g.Product)
                    .ThenInclude(p => p.Category)
                .Include(g => g.GRN)
                .AsQueryable();

            // Filter by search term (product name or item code)
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(g => 
                    g.Product.Name.Contains(search) || 
                    (g.ItemCode != null && g.ItemCode.Contains(search)));
            }

            // Filter by category
            if (categoryId.HasValue)
            {
                query = query.Where(g => g.Product.CategoryId == categoryId.Value);
            }

            // Filter by low stock
            if (lowStock)
            {
                query = query.Where(g => g.CurrentStockQuantity <= g.Product.MinStockLevel);
            }

            // Filter by expiring soon (within 30 days)
            if (expiringSoon)
            {
                var thirtyDaysFromNow = DateTime.Now.AddDays(30);
                query = query.Where(g => g.ExpiryDate.HasValue && g.ExpiryDate <= thirtyDaysFromNow);
            }

            var stockDetails = await query
                .OrderBy(g => g.Product.Name)
                .ToListAsync();

            // Get categories for dropdown
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.LowStock = lowStock;
            ViewBag.ExpiringSoon = expiringSoon;

            return View(stockDetails);
        }

        public async Task<IActionResult> Details(int id)
        {
            var stockDetail = await _context.GRNDetails
                .Include(g => g.Product)
                    .ThenInclude(p => p.Category)
                .Include(g => g.Product.UOM)
                .Include(g => g.GRN)
                    .ThenInclude(grn => grn.PurchaseOrder)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (stockDetail == null) return NotFound();

            return View(stockDetail);
        }
    }
}
