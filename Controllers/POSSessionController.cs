using CityMarketPOS.Models;
using CityMarketPOS.Models.ViewModels;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Cashier")]
    public class POSSessionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAuditLogRepository _auditLogRepo;

        public POSSessionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IAuditLogRepository auditLogRepo)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _auditLogRepo = auditLogRepo;
        }

        public async Task<IActionResult> SelectCounter()
        {
            // Check if user already has an active session
            var userId = _userManager.GetUserId(User);
            var activeSession = await _context.POSSessions
                .Include(s => s.Counter)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

            if (activeSession != null)
            {
                // Redirect to POS Terminal if already has active session
                return RedirectToAction("Index", "POSTerminal");
            }

            // Get available active counters
            var availableCounters = await _context.Counters
                .Where(c => c.Status == "Active")
                .ToListAsync();

            return View(availableCounters);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectCounter(int counterId)
        {
            var userId = _userManager.GetUserId(User);
            var userName = _userManager.GetUserName(User);

            // Check if counter exists and is active
            var counter = await _context.Counters.FindAsync(counterId);
            if (counter == null || counter.Status != "Active")
            {
                TempData["Error"] = "Invalid or inactive counter selected.";
                return RedirectToAction(nameof(SelectCounter));
            }

            // Check if counter is already assigned to another active session
            var existingSession = await _context.POSSessions
                .FirstOrDefaultAsync(s => s.CounterId == counterId && s.Status == "Active");

            if (existingSession != null)
            {
                TempData["Error"] = "This counter is already in use by another cashier.";
                return RedirectToAction(nameof(SelectCounter));
            }

            // Create new POS session
            var session = new POSSession
            {
                UserId = userId,
                UserName = userName,
                CounterId = counterId,
                StartTime = DateTime.Now,
                Status = "Active"
            };

            _context.POSSessions.Add(session);
            await _context.SaveChangesAsync();

            // Store session ID in session
            HttpContext.Session.SetInt32("POSSessionId", session.Id);
            HttpContext.Session.SetInt32("CounterId", counterId);

            var user = await _userManager.GetUserAsync(User);
            await _auditLogRepo.LogAsync("POSSession", session.Id.ToString(), "Create", user?.Id ?? "System", user?.UserName ?? "System", $"Started POS session at counter: {counter.Name}");

            return RedirectToAction("Index", "POSTerminal");
        }

        public async Task<IActionResult> EndSession()
        {
            var userId = _userManager.GetUserId(User);
            var session = await _context.POSSessions
                .Include(s => s.Counter)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

            if (session != null)
            {
                // Get all sales for this session
                var sales = await _context.Sales
                    .Where(s => s.POSSessionId == session.Id)
                    .Include(s => s.Details)
                    .ToListAsync();

                var viewModel = new SessionReportViewModel
                {
                    Session = session,
                    Sales = sales,
                    TotalSales = sales.Count,
                    TotalAmount = sales.Sum(s => s.GrandTotal),
                    TotalItems = sales.Sum(s => s.Details.Sum(d => d.Quantity))
                };

                return View("EndSession", viewModel);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEndSession(int sessionId)
        {
            var userId = _userManager.GetUserId(User);
            var session = await _context.POSSessions
                .Include(s => s.Counter)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active" && s.Id == sessionId);

            if (session != null)
            {
                session.Status = "Closed";
                session.EndTime = DateTime.Now;
                _context.POSSessions.Update(session);
                await _context.SaveChangesAsync();

                // Clear session
                HttpContext.Session.Clear();

                var user = await _userManager.GetUserAsync(User);
                await _auditLogRepo.LogAsync("POSSession", session.Id.ToString(), "Close", user?.Id ?? "System", user?.UserName ?? "System", $"Ended POS session at counter: {session.Counter.Name}");

                // Logout
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
