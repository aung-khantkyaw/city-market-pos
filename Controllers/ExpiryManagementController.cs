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

        public async Task<IActionResult> Reorder(int id)
        {
            var grnDetail = await _context.GRNDetails
                .Include(g => g.Product)
                .Include(g => g.GRN)
                    .ThenInclude(grn => grn.PurchaseOrder)
                        .ThenInclude(po => po.Supplier)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grnDetail == null) return NotFound();

            // Prepare PO details for confirmation dialog
            var suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();

            ViewBag.GRNDetail = grnDetail;
            ViewBag.Suppliers = suppliers;
            ViewBag.PONumber = "PO-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            ViewBag.SelectedSupplierId = grnDetail.GRN?.PurchaseOrder?.SupplierId;
            ViewBag.Quantity = grnDetail.CurrentStockQuantity;
            ViewBag.UnitPrice = grnDetail.PurchasePrice;
            ViewBag.TotalPrice = grnDetail.CurrentStockQuantity * grnDetail.PurchasePrice;

            return View("ReorderConfirm");
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmReorder(int grnDetailId, string poNumber, int supplierId, List<PurchaseOrderDetailItem> items)
        {
            var grnDetail = await _context.GRNDetails
                .Include(g => g.Product)
                .FirstOrDefaultAsync(g => g.Id == grnDetailId);

            if (grnDetail == null) return NotFound();

            if (items == null || !items.Any())
            {
                TempData["Error"] = "Please add at least one product to the order.";
                return RedirectToAction("Reorder", new { id = grnDetailId });
            }

            // Calculate total amount
            var totalAmount = items.Sum(i => i.Quantity * i.UnitPrice);

            // Create a new purchase order with the confirmed details
            var purchaseOrder = new PurchaseOrder
            {
                PONumber = poNumber,
                OrderDate = DateTime.Now,
                ExpectedDate = DateTime.Now.AddDays(7),
                SupplierId = supplierId,
                Status = "Pending",
                TotalAmount = totalAmount
            };

            _context.PurchaseOrders.Add(purchaseOrder);
            await _context.SaveChangesAsync();

            // Add products to purchase order details
            foreach (var item in items)
            {
                var purchaseOrderDetail = new PurchaseOrderDetail
                {
                    PurchaseOrderId = purchaseOrder.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };

                _context.PurchaseOrderDetails.Add(purchaseOrderDetail);
            }
            await _context.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);
            await _auditLogRepo.LogAsync("ExpiryManagement", grnDetailId.ToString(), "Reorder", user?.Id ?? "System", user?.UserName ?? "System", $"Created reorder PO #{purchaseOrder.PONumber} for {grnDetail.Product.Name} (Qty: {items.Sum(i => i.Quantity)})");

            TempData["Success"] = $"Purchase Order #{purchaseOrder.PONumber} created successfully!";
            return RedirectToAction("Details", "PurchaseOrder", new { id = purchaseOrder.Id });
        }

        public class PurchaseOrderDetailItem
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
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
