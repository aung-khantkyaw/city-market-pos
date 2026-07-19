using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager,Inventory")]
    public class StockAdjustmentController : Controller
    {
        private readonly IStockAdjustmentRepository _adjustmentRepo;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public StockAdjustmentController(IStockAdjustmentRepository adjustmentRepo, ApplicationDbContext context, IAuditLogRepository auditLogRepo, UserManager<ApplicationUser> userManager)
        {
            _adjustmentRepo = adjustmentRepo;
            _context = context;
            _auditLogRepo = auditLogRepo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var adjustments = await _adjustmentRepo.GetAllAsync();
            return View(adjustments);
        }

        public IActionResult Create()
        {
            var grnDetails = _context.GRNDetails
                .Include(g => g.Product)
                .Include(g => g.GRN)
                .ThenInclude(g => g.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
                .Where(g => g.CurrentStockQuantity > 0)
                .OrderBy(g => g.Product.Name)
                .Select(g => new
                {
                    Id = g.Id,
                    DisplayText = $"{g.Product.Name} - {g.GRN.PurchaseOrder.Supplier.Name ?? "N/A"} (Stock: {g.CurrentStockQuantity})"
                })
                .ToList();

            ViewBag.GRNDetails = new SelectList(grnDetails, "Id", "DisplayText");
            
            ViewBag.AdjustmentTypes = new SelectList(new[]
            {
                new { Value = "In", Text = "Stock In" },
                new { Value = "Out", Text = "Stock Out" },
                new { Value = "Damage", Text = "Damage" },
                new { Value = "Loss", Text = "Loss" },
                new { Value = "Theft", Text = "Theft" },
                new { Value = "Correction", Text = "Correction" }
            }, "Value", "Text");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockAdjustment adjustment)
        {
            // 1. Set your backend-generated properties
            adjustment.AdjustedByUserId = _userManager.GetUserId(User);
            adjustment.AdjustedByUserName = _userManager.GetUserName(User);
            adjustment.AdjustmentDate = DateTime.Now;

            // 2. Clear the validation errors for navigation properties and fields we set manually
            ModelState.Remove(nameof(adjustment.GRNDetail));
            ModelState.Remove(nameof(adjustment.AdjustedByUserId));
            ModelState.Remove(nameof(adjustment.AdjustedByUserName));

            // 3. Now check if the rest of the user-submitted form is valid
            if (ModelState.IsValid)
            {
                var grnDetail = await _context.GRNDetails
                    .Include(g => g.Product)
                    .FirstOrDefaultAsync(g => g.Id == adjustment.GRNDetailId);

                if (grnDetail == null)
                {
                    ModelState.AddModelError("", "Stock item not found");
                    return View(adjustment);
                }

                // Update stock quantity based on adjustment type
                if (adjustment.AdjustmentType == "In" || adjustment.AdjustmentType == "Correction")
                {
                    grnDetail.CurrentStockQuantity += adjustment.Quantity;
                }
                else
                {
                    if (grnDetail.CurrentStockQuantity < adjustment.Quantity)
                    {
                        ModelState.AddModelError("", "Insufficient stock for this adjustment");
                        return View(adjustment);
                    }
                    grnDetail.CurrentStockQuantity -= adjustment.Quantity;
                }

                _context.GRNDetails.Update(grnDetail);
                await _adjustmentRepo.AddAsync(adjustment);

                var user = await _userManager.GetUserAsync(User);
                await _auditLogRepo.LogAsync("StockAdjustment", adjustment.Id.ToString(), "Create", user?.Id ?? "System", user?.UserName ?? "System", $"Stock {adjustment.AdjustmentType}: {adjustment.Quantity} units for {grnDetail.Product.Name} - {adjustment.Reason}");

                TempData["Success"] = "Stock adjustment recorded successfully!";
                return RedirectToAction(nameof(Index));
            }

            // If we reach here, something else failed validation. Repopulate dropdowns.
            var grnDetails = _context.GRNDetails
                .Include(g => g.Product)
                .Include(g => g.GRN)
                .ThenInclude(g => g.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
                .Where(g => g.CurrentStockQuantity > 0)
                .OrderBy(g => g.Product.Name)
                .Select(g => new
                {
                    Id = g.Id,
                    DisplayText = $"{g.Product.Name} - {g.GRN.PurchaseOrder.Supplier.Name ?? "N/A"} (Stock: {g.CurrentStockQuantity})"
                })
                .ToList();

            ViewBag.GRNDetails = new SelectList(grnDetails, "Id", "DisplayText");

            ViewBag.AdjustmentTypes = new SelectList(new[]
            {
        new { Value = "In", Text = "Stock In" },
        new { Value = "Out", Text = "Stock Out" },
        new { Value = "Damage", Text = "Damage" },
        new { Value = "Loss", Text = "Loss" },
        new { Value = "Theft", Text = "Theft" },
        new { Value = "Correction", Text = "Correction" }
    }, "Value", "Text");

            return View(adjustment);
        }

        public async Task<IActionResult> Details(int id)
        {
            var adjustment = await _adjustmentRepo.GetByIdAsync(id);
            if (adjustment == null) return NotFound();
            return View(adjustment);
        }
    }
}
