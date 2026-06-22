using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager,Inventory")]
    public class UOMController : Controller
    {
        private readonly IUOMRepository _uomRepo;

        public UOMController(IUOMRepository uomRepo)
        {
            _uomRepo = uomRepo;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _uomRepo.GetAllAsync());
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UOM uom)
        {
            if (ModelState.IsValid)
            {
                await _uomRepo.AddAsync(uom);
                await _uomRepo.SaveChangesAsync();
                TempData["Success"] = "UOM created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(uom);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UOM uom)
        {
            if (id != uom.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _uomRepo.Update(uom);
                await _uomRepo.SaveChangesAsync();
                TempData["Success"] = "UOM updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(uom);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var uom = await _uomRepo.GetByIdAsync(id);
            if (uom != null)
            {
                _uomRepo.Delete(uom);
                await _uomRepo.SaveChangesAsync();
                TempData["Success"] = "UOM deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}