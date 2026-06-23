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

        public IActionResult Create()
        {
            var model = new PurchaseOrderCreateViewModel
            {
                PONumber = "PO-" + DateTime.Now.ToString("yyyyMMddHHmmss")
            };

            ViewBag.ProductList = new SelectList(_context.Products, "Id", "Name");

            return PartialView("_CreatePartial", model);
        }

        [HttpGet]
        public IActionResult GetSuppliersByProduct(int productId)
        {
            var suppliers = _context.Suppliers
                .Where(s => s.Products.Any(p => p.Id == productId))
                .Select(s => new { value = s.Id, text = s.Name })
                .ToList();

            return Json(suppliers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrderCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ProductList = new SelectList(_context.Products, "Id", "Name", model.ProductId);
                return View(model);
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

            po.OrderDetails = new List<PurchaseOrderDetail>
            {
                new PurchaseOrderDetail
                {
                    ProductId = model.ProductId,
                    Quantity = model.Quantity,
                }
            };

            await _poRepo.AddAsync(po);
            await _poRepo.SaveChangesAsync();

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