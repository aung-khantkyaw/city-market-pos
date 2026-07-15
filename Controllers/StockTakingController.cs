using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager,Inventory")]
    public class StockTakingController : Controller
    {
        private readonly IStockTakingRepository _stockTakingRepo;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public StockTakingController(IStockTakingRepository stockTakingRepo, ApplicationDbContext context, IAuditLogRepository auditLogRepo, UserManager<ApplicationUser> userManager)
        {
            _stockTakingRepo = stockTakingRepo;
            _context = context;
            _auditLogRepo = auditLogRepo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var stockTakings = await _stockTakingRepo.GetAllAsync();
            return View(stockTakings);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockTaking stockTaking)
        {
            if (ModelState.IsValid)
            {
                stockTaking.StockTakingNumber = "ST-" + DateTime.Now.ToString("yyyyMMdd-HHmm");
                stockTaking.ConductedByUserId = _userManager.GetUserId(User);
                stockTaking.ConductedByUserName = _userManager.GetUserName(User);
                stockTaking.TakingDate = DateTime.Now;
                stockTaking.Status = "In Progress";

                await _stockTakingRepo.AddAsync(stockTaking);

                var user = await _userManager.GetUserAsync(User);
                await _auditLogRepo.LogAsync("StockTaking", stockTaking.Id.ToString(), "Create", user?.Id ?? "System", user?.UserName ?? "System", $"Created Stock Taking: {stockTaking.StockTakingNumber}");

                return RedirectToAction(nameof(AddDetails), new { id = stockTaking.Id });
            }
            return View(stockTaking);
        }

        public async Task<IActionResult> AddDetails(int id)
        {
            var stockTaking = await _stockTakingRepo.GetByIdAsync(id);
            if (stockTaking == null) return NotFound();

            if (stockTaking.Status != "In Progress")
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.StockTaking = stockTaking;
            ViewBag.GRNDetails = await _context.GRNDetails
                .Include(g => g.Product)
                .Where(g => g.CurrentStockQuantity > 0)
                .OrderBy(g => g.Product.Name)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDetails(int id, int grnDetailId, int actualQuantity, string notes)
        {
            var stockTaking = await _stockTakingRepo.GetByIdAsync(id);
            if (stockTaking == null) return NotFound();

            var grnDetail = await _context.GRNDetails.FindAsync(grnDetailId);
            if (grnDetail == null) return NotFound();

            // Check if this detail already exists
            var existingDetail = stockTaking.Details.FirstOrDefault(d => d.GRNDetailId == grnDetailId);
            if (existingDetail != null)
            {
                ModelState.AddModelError("", "This item has already been counted");
                ViewBag.StockTaking = stockTaking;
                ViewBag.GRNDetails = await _context.GRNDetails
                    .Include(g => g.Product)
                    .Where(g => g.CurrentStockQuantity > 0)
                    .OrderBy(g => g.Product.Name)
                    .ToListAsync();
                return View();
            }

            var detail = new StockTakingDetail
            {
                StockTakingId = id,
                GRNDetailId = grnDetailId,
                ExpectedQuantity = grnDetail.CurrentStockQuantity,
                ActualQuantity = actualQuantity,
                Variance = actualQuantity - grnDetail.CurrentStockQuantity,
                Notes = notes
            };

            stockTaking.Details.Add(detail);
            _context.StockTakingDetails.Add(detail);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(AddDetails), new { id });
        }

        public async Task<IActionResult> Complete(int id)
        {
            var stockTaking = await _stockTakingRepo.GetByIdAsync(id);
            if (stockTaking == null) return NotFound();

            stockTaking.Status = "Completed";
            stockTaking.CompletedDate = DateTime.Now;

            await _stockTakingRepo.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);
            await _auditLogRepo.LogAsync("StockTaking", stockTaking.Id.ToString(), "Complete", user?.Id ?? "System", user?.UserName ?? "System", $"Completed Stock Taking: {stockTaking.StockTakingNumber} with {stockTaking.Details.Count} items");

            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var stockTaking = await _stockTakingRepo.GetByIdAsync(id);
            if (stockTaking == null) return NotFound();
            return View(stockTaking);
        }
    }
}
