using CityMarketPOS.Models;
using CityMarketPOS.Models.ViewModels;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CityMarketPOS.Controllers
{
    public class PurchaseOrderController : Controller
    {
        private readonly IPurchaseOrderRepository _poRepo;
        private readonly ApplicationDbContext _context;

        public PurchaseOrderController(IPurchaseOrderRepository poRepo, ApplicationDbContext context)
        {
            _poRepo = poRepo;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var pos = await _poRepo.GetAllPOAsync();
            return View(pos);
        }


        [HttpGet]
        public IActionResult GetProductsBySupplier(int supplierId)
        {
            var products = _context.Products
                .Where(p => p.Suppliers.Any(s => s.Id == supplierId)) 
                .Select(p => new { value = p.Id, text = p.Name })
                .ToList();

            return Json(products);
        }

        public IActionResult Create()
        {
            var model = new PurchaseOrderCreateViewModel
            {
                PONumber = "PO-" + DateTime.Now.ToString("yyyyMMddHHmmss")
            };

            ViewBag.SupplierList = new SelectList(_context.Suppliers, "Id", "Name");
            ViewBag.ProductList = new SelectList(_context.Products, "Id", "Name");

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

            var po = new PurchaseOrder
            {
                PONumber = model.PONumber,
                SupplierId = model.SupplierId,
                Status = "Pending",
                OrderDate = DateTime.Now,
                ExpectedDate = DateTime.Now.AddDays(7),
                TotalAmount = 0
            };

            po.OrderDetails = model.Items.Select(item => new PurchaseOrderDetail
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList();

            await _poRepo.AddAsync(po);
            await _poRepo.SaveChangesAsync();

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