using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager,Inventory")]
    public class UOMController : Controller
    {
        private readonly IUOMRepository _uomRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public UOMController(IUOMRepository uomRepo, IAuditLogRepository auditLogRepo, UserManager<ApplicationUser> userManager)
        {
            _uomRepo = uomRepo;
            _auditLogRepo = auditLogRepo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _uomRepo.GetAllAsync());
        }
        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UOM uom)
        {
            if (ModelState.IsValid)
            {
                await _uomRepo.AddAsync(uom);
                await _uomRepo.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                await _auditLogRepo.LogAsync("UOM", uom.Id.ToString(), "Create", user?.Id ?? "System", user?.UserName ?? "System", $"Created UOM: {uom.Name} ({uom.ShortName})");

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
                var oldUOM = await _uomRepo.GetByIdAsync(id);
                _uomRepo.Update(uom);
                await _uomRepo.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                await _auditLogRepo.LogAsync("UOM", uom.Id.ToString(), "Update", user?.Id ?? "System", user?.UserName ?? "System", $"Updated UOM: {uom.Name} ({uom.ShortName})", oldValues: $"Old: {oldUOM?.Name} ({oldUOM?.ShortName})", newValues: $"New: {uom.Name} ({uom.ShortName})");

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
                uom.IsDeleted = true;
                _uomRepo.Update(uom);
                await _uomRepo.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                await _auditLogRepo.LogAsync("UOM", id.ToString(), "Soft Delete", user?.Id ?? "System", user?.UserName ?? "System", $"Soft deleted UOM: {uom.Name}");

                TempData["Success"] = "UOM removed successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}