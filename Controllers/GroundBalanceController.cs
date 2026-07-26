using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager,Inventory")]
    public class GroundBalanceController : Controller
    {
        private readonly IGroundBalanceRepository _gbRepo;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogRepository _auditLogRepo;

        public GroundBalanceController(IGroundBalanceRepository gbRepo, ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditLogRepository auditLogRepo)
        {
            _gbRepo = gbRepo;
            _context = context;
            _userManager = userManager;
            _auditLogRepo = auditLogRepo;
        }

        public async Task<IActionResult> Index(DateTime? gbDate, string status)
        {
            var query = _context.GroundBalances
                .Include(g => g.GroundBalanceDetails)
                .AsQueryable();

            if (gbDate.HasValue)
            {
                query = query.Where(g => g.GBDate.Date == gbDate.Value.Date);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(g => g.Status == status);
            }

            var groundBalances = await query.OrderByDescending(g => g.GBDate).ToListAsync();

            ViewBag.StatusList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Statuses" },
                new SelectListItem { Value = "Pending", Text = "Pending" },
                new SelectListItem { Value = "Completed", Text = "Completed" },
                new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
            };

            return View(groundBalances);
        }

        public IActionResult Create()
        {
            var model = new GroundBalance
            {
                BatchNo = "GB-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                GBDate = DateTime.Now,
                Status = "Pending"
            };

            ViewBag.ItemCodeList = new SelectList(_context.GRNDetails.Include(g => g.Product).Where(g => g.CurrentStockQuantity > 0), "ItemCode", "ItemCode");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GroundBalance groundBalance, string[] ItemCode, float[] ActualQty, float[] InventoryQty, string[] ItemRemarks)
        {
            if (ItemCode == null || ItemCode.Length == 0)
            {
                ModelState.AddModelError("", "Please add at least one item.");
                ViewBag.ItemCodeList = new SelectList(_context.GRNDetails.Include(g => g.Product).Where(g => g.CurrentStockQuantity > 0), "ItemCode", "ItemCode");
                return View(groundBalance);
            }

            var user = await _userManager.GetUserAsync(User);
            groundBalance.CreatedUser = user?.Id ?? "System";

            groundBalance.GroundBalanceDetails = new List<GroundBalanceDetail>();

            for (int i = 0; i < ItemCode.Length; i++)
            {
                if (!string.IsNullOrEmpty(ItemCode[i]))
                {
                    // Get inventory quantity from GRNDetails
                    var grnDetail = await _context.GRNDetails.FirstOrDefaultAsync(g => g.ItemCode == ItemCode[i]);
                    float inventoryQty = grnDetail?.CurrentStockQuantity ?? 0;

                    groundBalance.GroundBalanceDetails.Add(new GroundBalanceDetail
                    {
                        BatchNo = groundBalance.BatchNo,
                        ItemCode = ItemCode[i],
                        ActualQty = ActualQty[i],
                        InventoryQty = inventoryQty,
                        DifferentQty = ActualQty[i] - inventoryQty,
                        Remark = ItemRemarks[i],
                        CreatedUser = user?.Id ?? "System"
                    });
                }
            }

            await _gbRepo.AddAsync(groundBalance);

            await _auditLogRepo.LogAsync("GroundBalance", groundBalance.BatchNo, "Create", user?.Id ?? "System", user?.UserName ?? "System", $"Created Ground Balance: {groundBalance.BatchNo} with {groundBalance.GroundBalanceDetails.Count} items");

            TempData["Success"] = "Ground Balance created successfully!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string batchNo)
        {
            var groundBalance = await _gbRepo.GetByBatchNoAsync(batchNo);
            if (groundBalance == null) return NotFound();
            return View(groundBalance);
        }

        public async Task<IActionResult> Edit(string batchNo)
        {
            var groundBalance = await _gbRepo.GetByBatchNoAsync(batchNo);
            if (groundBalance == null) return NotFound();

            ViewBag.ItemCodeList = new SelectList(_context.GRNDetails.Include(g => g.Product).Where(g => g.CurrentStockQuantity > 0), "ItemCode", "ItemCode");

            return View(groundBalance);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string batchNo, GroundBalance groundBalance, int[] DetailId, string[] ItemCode, float[] ActualQty, float[] InventoryQty, string[] ItemRemarks)
        {
            if (batchNo != groundBalance.BatchNo) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingGb = await _context.GroundBalances
                        .Include(g => g.GroundBalanceDetails)
                        .FirstOrDefaultAsync(g => g.BatchNo == batchNo);

                    if (existingGb == null) return NotFound();

                    existingGb.Remark = groundBalance.Remark;
                    existingGb.GBDate = groundBalance.GBDate;
                    existingGb.Status = groundBalance.Status;
                    existingGb.UpdatedDate = DateTime.Now;

                    var user = await _userManager.GetUserAsync(User);
                    existingGb.UpdatedUser = user?.Id ?? "System";

                    // Remove existing details
                    _context.GroundBalanceDetails.RemoveRange(existingGb.GroundBalanceDetails);

                    // Add new details
                    for (int i = 0; i < ItemCode.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(ItemCode[i]))
                        {
                            // Get inventory quantity from GRNDetails
                            var grnDetail = await _context.GRNDetails.FirstOrDefaultAsync(g => g.ItemCode == ItemCode[i]);
                            float inventoryQty = grnDetail?.CurrentStockQuantity ?? 0;

                            existingGb.GroundBalanceDetails.Add(new GroundBalanceDetail
                            {
                                BatchNo = groundBalance.BatchNo,
                                ItemCode = ItemCode[i],
                                ActualQty = ActualQty[i],
                                InventoryQty = inventoryQty,
                                DifferentQty = ActualQty[i] - inventoryQty,
                                Remark = ItemRemarks[i],
                                UpdatedUser = user?.Id ?? "System",
                                UpdatedDate = DateTime.Now
                            });
                        }
                    }

                    await _context.SaveChangesAsync();

                    await _auditLogRepo.LogAsync("GroundBalance", groundBalance.BatchNo, "Edit", user?.Id ?? "System", user?.UserName ?? "System", $"Updated Ground Balance: {groundBalance.BatchNo}");

                    TempData["Success"] = "Ground Balance updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while updating the ground balance.");
                }
            }

            ViewBag.ItemCodeList = new SelectList(_context.GRNDetails.Include(g => g.Product).Where(g => g.CurrentStockQuantity > 0), "ItemCode", "ItemCode");
            return View(groundBalance);
        }

        public async Task<IActionResult> Delete(string batchNo)
        {
            var groundBalance = await _gbRepo.GetByBatchNoAsync(batchNo);
            if (groundBalance == null) return NotFound();
            return View(groundBalance);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string batchNo)
        {
            var groundBalance = await _gbRepo.GetByBatchNoAsync(batchNo);
            if (groundBalance != null)
            {
                var user = await _userManager.GetUserAsync(User);
                await _auditLogRepo.LogAsync("GroundBalance", groundBalance.BatchNo, "Delete", user?.Id ?? "System", user?.UserName ?? "System", $"Deleted Ground Balance: {groundBalance.BatchNo}");

                await _gbRepo.DeleteAsync(batchNo);
                TempData["Success"] = "Ground Balance deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult GetInventoryQty(string itemCode)
        {
            var grnDetail = _context.GRNDetails.FirstOrDefault(g => g.ItemCode == itemCode);
            float inventoryQty = grnDetail?.CurrentStockQuantity ?? 0;
            return Json(new { inventoryQty = inventoryQty });
        }
    }
}
