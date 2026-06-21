using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name");
            ViewBag.UOMs = new SelectList(_context.UOMs, "Id", "Name");
            ViewBag.GeneratedBarcode = _prodRepo.GenerateBarcode();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            product.Barcode = _prodRepo.GenerateBarcode();

            ModelState.Remove("Category");
            ModelState.Remove("Brand");
            ModelState.Remove("UOM");

            if (ModelState.IsValid)
            {
                await _prodRepo.AddAsync(product);
                await _prodRepo.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name");
            ViewBag.UOMs = new SelectList(_context.UOMs, "Id", "Name");
            return View(product);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _prodRepo.GetByIdAsync(id);
            if (product == null) return NotFound();
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            ViewBag.UOMs = new SelectList(_context.UOMs, "Id", "Name", product.UOMId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _prodRepo.Update(product);
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
            ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            ViewBag.UOMs = new SelectList(_context.UOMs, "Id", "Name", product.UOMId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var prod = await _prodRepo.GetByIdAsync(id);
            if (prod != null) { _prodRepo.Delete(prod); await _prodRepo.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }
    }
}