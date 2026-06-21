using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager,Inventory")]
    public class BrandController : Controller
    {
        private readonly IBrandRepository _brandRepo;

        public BrandController(IBrandRepository brandRepo)
        {
            _brandRepo = brandRepo;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _brandRepo.GetAllAsync());
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand)
        {
            if (ModelState.IsValid)
            {
                await _brandRepo.AddAsync(brand);
                await _brandRepo.SaveChangesAsync();
                TempData["Success"] = "Brand created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _brandRepo.GetByIdAsync(id);
            if (brand == null) return NotFound();
            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand brand)
        {
            if (id != brand.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _brandRepo.Update(brand);
                await _brandRepo.SaveChangesAsync();
                TempData["Success"] = "Brand updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _brandRepo.GetByIdAsync(id);
            if (brand != null)
            {
                _brandRepo.Delete(brand);
                await _brandRepo.SaveChangesAsync();
                TempData["Success"] = "Brand deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}