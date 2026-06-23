using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager,Inventory")]
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepo;

        public CategoryController(ICategoryRepository categoryRepo)
        {
            _categoryRepo = categoryRepo;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepo.GetAllAsync();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                if (!await _categoryRepo.IsShortNameUniqueAsync(category.ShortName))
                {
                    TempData["Error"] = "Category Short Name already exists!";
                    return RedirectToAction(nameof(Index));
                }

                await _categoryRepo.AddAsync(category);
                await _categoryRepo.SaveChangesAsync();
                TempData["Success"] = "Category created successfully!";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Invalid data submitted!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id) return NotFound();

            if (ModelState.IsValid)
            {
                if (!await _categoryRepo.IsShortNameUniqueAsync(category.ShortName, category.Id))
                {
                    TempData["Error"] = "Category Short Name already exists!";
                    return RedirectToAction(nameof(Index));
                }

                _categoryRepo.Update(category);
                await _categoryRepo.SaveChangesAsync();
                TempData["Success"] = "Category updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Invalid data submitted!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category != null)
            {
                _categoryRepo.Delete(category);
                await _categoryRepo.SaveChangesAsync();
                TempData["Success"] = "Category deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}