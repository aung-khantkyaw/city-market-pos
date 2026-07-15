using CityMarketPOS.Models;
using CityMarketPOS.Models.ViewModels;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager,Inventory")]
    public class PurchaseOrderController : Controller
    {
        private readonly IPurchaseOrderRepository _poRepo;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public PurchaseOrderController(IPurchaseOrderRepository poRepo, ApplicationDbContext context, IAuditLogRepository auditLogRepo, UserManager<ApplicationUser> userManager)
        {
            _poRepo = poRepo;
            _context = context;
            _auditLogRepo = auditLogRepo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var pos = await _poRepo.GetAllPOAsync();
            return View(pos);
        }


        [HttpGet]
        public IActionResult GetCategoriesBySupplier(int supplierId)
        {
            var categories = _context.Products
                .Where(p => p.Suppliers.Any(s => s.Id == supplierId) && p.Category != null)
                .Select(p => new { value = p.Category.Id, text = p.Category.Name })
                .Distinct()
                .ToList();

            var uniqueCategories = categories
                .GroupBy(c => c.value)
                .Select(g => g.First())
                .ToList();

            return Json(uniqueCategories);
        }

        [HttpGet]
        public IActionResult GetProductsBySupplierAndCategory(int supplierId, int categoryId)
        {
            var products = _context.Products
                .Where(p => p.Suppliers.Any(s => s.Id == supplierId) && p.CategoryId == categoryId)
                .Select(p => new { value = p.Id, text = p.Name })
                .Distinct()
                .ToList();

            var uniqueProducts = products
                .GroupBy(p => p.value)
                .Select(g => g.First())
                .ToList();

            return Json(uniqueProducts);
        }

        public IActionResult Create()
        {
            var model = new PurchaseOrderCreateViewModel
            {
                PONumber = "PO-" + DateTime.Now.ToString("yyyyMMddHHmmss")
            };

            ViewBag.SupplierList = new SelectList(_context.Suppliers, "Id", "Name");

            return PartialView("_CreatePartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrderCreateViewModel model)
        {
            if (!ModelState.IsValid || model.Items == null || model.Items.Count == 0)
            {
                TempData["Error"] = "Please add at least one product with correct quantity.";
                return RedirectToAction(nameof(Index));
            }

            decimal calculatedTotalAmount = 0;
            var orderDetails = new List<PurchaseOrderDetail>();

            foreach (var item in model.Items)
            {
                orderDetails.Add(new PurchaseOrderDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice 
                });

                calculatedTotalAmount += (item.Quantity * item.UnitPrice);
            }

            var po = new PurchaseOrder
            {
                PONumber = model.PONumber,
                SupplierId = model.SupplierId,
                Status = "Pending",
                OrderDate = DateTime.Now,
                ExpectedDate = DateTime.Now.AddDays(7),

                TotalAmount = calculatedTotalAmount,

                OrderDetails = orderDetails
            };

            await _poRepo.AddAsync(po);
            await _poRepo.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);
            await _auditLogRepo.LogAsync("PurchaseOrder", po.Id.ToString(), "Create", user?.Id ?? "System", user?.UserName ?? "System", $"Created Purchase Order: {po.PONumber} with {orderDetails.Count} items, Total: {calculatedTotalAmount:F2}");

            TempData["Success"] = "Purchase Order created successfully!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var po = await _poRepo.GetByIdWithDetailsAsync(id);
            if (po == null) return NotFound();
            return View(po);
        }
    }
}