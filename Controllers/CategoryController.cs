using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager,Inventory")]
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public CategoryController(ICategoryRepository categoryRepo, IAuditLogRepository auditLogRepo, UserManager<ApplicationUser> userManager)
        {
            _categoryRepo = categoryRepo;
            _auditLogRepo = auditLogRepo;
            _userManager = userManager;
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

                var user = await _userManager.GetUserAsync(User);
                await _auditLogRepo.LogAsync("Category", category.Id.ToString(), "Create", user?.Id ?? "System", user?.UserName ?? "System", $"Created category: {category.Name} ({category.ShortName})");

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

                var oldCategory = await _categoryRepo.GetByIdAsync(id);
                _categoryRepo.Update(category);
                await _categoryRepo.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                await _auditLogRepo.LogAsync("Category", category.Id.ToString(), "Update", user?.Id ?? "System", user?.UserName ?? "System", $"Updated category: {category.Name} ({category.ShortName})", oldValues: $"Old: {oldCategory?.Name} ({oldCategory?.ShortName})", newValues: $"New: {category.Name} ({category.ShortName})");

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
                var categoryName = category.Name;
                _categoryRepo.Delete(category);
                await _categoryRepo.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                await _auditLogRepo.LogAsync("Category", id.ToString(), "Delete", user?.Id ?? "System", user?.UserName ?? "System", $"Deleted category: {categoryName}");

                TempData["Success"] = "Category deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}