using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager")]
    public class CounterController : Controller
    {
        private readonly ICounterRepository _counterRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public CounterController(ICounterRepository counterRepo, IAuditLogRepository auditLogRepo, UserManager<ApplicationUser> userManager)
        {
            _counterRepo = counterRepo;
            _auditLogRepo = auditLogRepo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var counters = await _counterRepo.GetAllAsync();
            return View(counters);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Counter counter)
        {
            if (ModelState.IsValid)
            {
                counter.CreatedDate = DateTime.Now;
                counter.CreatedByUserId = _userManager.GetUserId(User);
                counter.CreatedByUserName = _userManager.GetUserName(User);

                await _counterRepo.AddAsync(counter);

                var user = await _userManager.GetUserAsync(User);
                await _auditLogRepo.LogAsync("Counter", counter.Id.ToString(), "Create", user?.Id ?? "System", user?.UserName ?? "System", $"Created counter: {counter.Name}");

                return RedirectToAction(nameof(Index));
            }
            return View(counter);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var counter = await _counterRepo.GetByIdAsync(id);
            if (counter == null) return NotFound();
            return View(counter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Counter counter)
        {
            if (id != counter.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existingCounter = await _counterRepo.GetByIdAsync(id);
                if (existingCounter == null) return NotFound();

                var oldValues = $"Name: {existingCounter.Name}, Location: {existingCounter.Location}, Status: {existingCounter.Status}";
                var newValues = $"Name: {counter.Name}, Location: {counter.Location}, Status: {counter.Status}";

                existingCounter.Name = counter.Name;
                existingCounter.Location = counter.Location;
                existingCounter.Status = counter.Status;
                existingCounter.AssignedUserId = counter.AssignedUserId;
                existingCounter.AssignedUserName = counter.AssignedUserName;
                existingCounter.Description = counter.Description;
                existingCounter.ModifiedDate = DateTime.Now;
                existingCounter.ModifiedByUserId = _userManager.GetUserId(User);
                existingCounter.ModifiedByUserName = _userManager.GetUserName(User);

                _counterRepo.Update(existingCounter);
                await _counterRepo.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                await _auditLogRepo.LogAsync("Counter", counter.Id.ToString(), "Edit", user?.Id ?? "System", user?.UserName ?? "System", $"Updated counter: {counter.Name}", oldValues, newValues);

                return RedirectToAction(nameof(Index));
            }
            return View(counter);
        }

        public async Task<IActionResult> Details(int id)
        {
            var counter = await _counterRepo.GetByIdAsync(id);
            if (counter == null) return NotFound();
            return View(counter);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var counter = await _counterRepo.GetByIdAsync(id);
            if (counter == null) return NotFound();
            return View(counter);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var counter = await _counterRepo.GetByIdAsync(id);
            if (counter == null) return NotFound();

            _counterRepo.Delete(counter);
            await _counterRepo.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);
            await _auditLogRepo.LogAsync("Counter", counter.Id.ToString(), "Delete", user?.Id ?? "System", user?.UserName ?? "System", $"Deleted counter: {counter.Name}");

            return RedirectToAction(nameof(Index));
        }
    }
}
