using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager,Inventory")]
    public class ProductController : Controller
    {
        private readonly IProductRepository _prodRepo;
        private readonly ApplicationDbContext _context;

        public ProductController(IProductRepository prodRepo, ApplicationDbContext context)
        {
            _prodRepo = prodRepo;
            _context = context;
        }

        public async Task<IActionResult> Index() => View(await _prodRepo.GetAllAsync());

        // GET: Product/Create (Partial View တောင်းဆိုမှုအတွက်)
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.UOMs = new SelectList(_context.UOMs, "Id", "Name");
            return PartialView("_CreatePartial");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            ModelState.Remove("Category");
            ModelState.Remove("UOM");

            if (ModelState.IsValid)
            {
                await _prodRepo.AddAsync(product);
                await _prodRepo.SaveChangesAsync();
                TempData["Success"] = "Product added successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.UOMs = new SelectList(_context.UOMs, "Id", "Name", product.UOMId);
            return PartialView("_CreatePartial", product);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _prodRepo.GetByIdAsync(id);
            if (product == null) return NotFound();

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.UOMs = new SelectList(_context.UOMs, "Id", "Name", product.UOMId);
            return PartialView("_EditPartial", product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id) return NotFound();

            ModelState.Remove("Category");
            ModelState.Remove("UOM");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = await _prodRepo.GetByIdAsync(id);
                    if (existingProduct == null) return NotFound();

                    existingProduct.Name = product.Name;
                    existingProduct.CategoryId = product.CategoryId;
                    existingProduct.UOMId = product.UOMId;
                    existingProduct.MinStockLevel = product.MinStockLevel;

                    _prodRepo.Update(existingProduct);
                    await _prodRepo.SaveChangesAsync();
                    TempData["Success"] = "Product updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    return NotFound();
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.UOMs = new SelectList(_context.UOMs, "Id", "Name", product.UOMId);
            return PartialView("_EditPartial", product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var prod = await _prodRepo.GetByIdAsync(id);
            if (prod != null)
            {
                _prodRepo.Delete(prod);
                await _prodRepo.SaveChangesAsync();
                TempData["Success"] = "Product deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}