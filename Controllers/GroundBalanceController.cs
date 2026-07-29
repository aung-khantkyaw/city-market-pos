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

            // Set item count for display
            ViewBag.ItemCount = _context.GRNDetails.Count(g => g.CurrentStockQuantity > 0);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GroundBalance groundBalance, string[] ItemCode, float[] ActualQty, float[] InventoryQty, string[] ItemRemarks)
        {
            if (ItemCode == null || ItemCode.Length == 0)
            {
                ModelState.AddModelError("", "Please add at least one item.");
                return View(groundBalance);
            }

            var user = await _userManager.GetUserAsync(User);
            groundBalance.CreatedUser = user?.Id ?? "System";
            groundBalance.Remark = groundBalance.Remark ?? "";
            groundBalance.UpdatedUser = "";

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
                        Remark = ItemRemarks[i] ?? "",
                        CreatedUser = user?.Id ?? "System",
                        UpdatedUser = ""
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
            // 1. Method စခေါ်တဲ့အချိန်မှာ Parameter တွေ မှန်မမှန် စစ်ဆေးရန်
            Console.WriteLine($"\n[Debug] === Edit POST Started for BatchNo: {batchNo} ===");
            Console.WriteLine($"[Debug] Received groundBalance.Status: {groundBalance.Status}");
            Console.WriteLine($"[Debug] Received groundBalance.GBDate: {groundBalance.GBDate}");

            if (batchNo != groundBalance.BatchNo)
            {
                Console.WriteLine($"[Debug] Error: URL batchNo ({batchNo}) and Model batchNo ({groundBalance.BatchNo}) mismatch!");
                return NotFound();
            }

            ModelState.Remove(nameof(GroundBalance.CreatedUser));
            ModelState.Remove(nameof(GroundBalance.UpdatedUser));
            ModelState.Remove(nameof(GroundBalance.GroundBalanceDetails));

            ModelState.Remove(nameof(GroundBalance.Remark));

            if (ItemCode == null || ItemCode.Length == 0 || !ItemCode.Any(itemCode => !string.IsNullOrWhiteSpace(itemCode)))
            {
                Console.WriteLine("[Debug] Warning: No valid items found in ItemCode array.");
                ModelState.AddModelError("", "Please add at least one item.");
            }

            // 2. ModelState.IsValid အခြေအနေကို စစ်ဆေးရန်
            Console.WriteLine($"[Debug] ModelState.IsValid: {ModelState.IsValid}");
            if (!ModelState.IsValid)
            {
                // 3. ဘာကြောင့် Invalid ဖြစ်နေလဲဆိုတဲ့ Error တွေကို ဖော်ပြပေးရန်
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        Console.WriteLine($"[Debug] ModelState Error: {error.ErrorMessage}");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    Console.WriteLine("[Debug] Fetching existing record from database...");
                    var existingGb = await _context.GroundBalances
                        .Include(g => g.GroundBalanceDetails)
                        .FirstOrDefaultAsync(g => g.BatchNo == batchNo);

                    if (existingGb == null)
                    {
                        Console.WriteLine($"[Debug] Error: Record {batchNo} not found in DB.");
                        return NotFound();
                    }

                    // 4. Update မလုပ်ခင် Old Value နဲ့ New Value ကို နှိုင်းယှဉ်ပြရန်
                    Console.WriteLine($"[Debug] Updating Status... Old: {existingGb.Status} -> New: {groundBalance.Status}");

                    existingGb.Remark = groundBalance.Remark ?? "";
                    existingGb.GBDate = groundBalance.GBDate;
                    existingGb.Status = groundBalance.Status;
                    existingGb.UpdatedDate = DateTime.Now;

                    var user = await _userManager.GetUserAsync(User);
                    existingGb.UpdatedUser = user?.Id ?? "System";

                    Console.WriteLine($"[Debug] Removing {existingGb.GroundBalanceDetails.Count} old details.");
                    _context.GroundBalanceDetails.RemoveRange(existingGb.GroundBalanceDetails);

                    // GroundBalanceDetails null ဖြစ်နေပါက အသစ် ဆောက်ပေးရန်
                    if (existingGb.GroundBalanceDetails == null)
                    {
                        existingGb.GroundBalanceDetails = new List<GroundBalanceDetail>();
                    }

                    Console.WriteLine($"[Debug] Processing {ItemCode.Length} items from form...");

                    // Add new details
                    for (int i = 0; i < ItemCode.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(ItemCode[i]))
                        {
                            var grnDetail = await _context.GRNDetails.FirstOrDefaultAsync(g => g.ItemCode == ItemCode[i]);
                            float inventoryQty = grnDetail?.CurrentStockQuantity ?? 0;

                            // Safe extraction: Array Null သို့မဟုတ် Index ရောက်မရောက် စစ်ဆေးခြင်း
                            float actual = (ActualQty != null && i < ActualQty.Length) ? ActualQty[i] : 0;
                            string remark = (ItemRemarks != null && i < ItemRemarks.Length) ? (ItemRemarks[i] ?? "") : "";

                            Console.WriteLine($"[Debug] Adding Item: {ItemCode[i]} | Actual: {actual} | Inv: {inventoryQty}");

                            existingGb.GroundBalanceDetails.Add(new GroundBalanceDetail
                            {
                                BatchNo = groundBalance.BatchNo,
                                ItemCode = ItemCode[i],
                                ActualQty = actual,
                                InventoryQty = inventoryQty,
                                DifferentQty = actual - inventoryQty,
                                Remark = remark,
                                CreatedUser = user?.Id ?? "System",
                                UpdatedUser = user?.Id ?? "System",
                                UpdatedDate = DateTime.Now
                            });
                        }
                    }

                    Console.WriteLine("[Debug] Saving changes to database (SaveChangesAsync)...");
                    await _context.SaveChangesAsync();
                    Console.WriteLine("[Debug] Successfully saved to database.");

                    await _auditLogRepo.LogAsync("GroundBalance", groundBalance.BatchNo, "Edit", user?.Id ?? "System", user?.UserName ?? "System", $"Updated Ground Balance: {groundBalance.BatchNo}");

                    TempData["Success"] = "Ground Balance updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // 6. Database သို့ သိမ်းရာတွင် Exception တက်ပါက အသေးစိတ်ဖော်ပြပေးရန်
                    Console.WriteLine("\n[Debug Error] Exception occurred while updating!");
                    Console.WriteLine($"[Debug Error Message]: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"[Debug Inner Exception]: {ex.InnerException.Message}");
                    }
                    Console.WriteLine($"[Debug StackTrace]: {ex.StackTrace}");

                    ModelState.AddModelError("", "An error occurred while updating the ground balance.");
                }
            }

            Console.WriteLine("[Debug] Returning to View (Validation failed or Exception caught).");
            groundBalance.GroundBalanceDetails = BuildPostedDetails(groundBalance.BatchNo, DetailId, ItemCode, ActualQty, InventoryQty, ItemRemarks);
            ViewBag.ItemCodeList = new SelectList(_context.GRNDetails.Include(g => g.Product).Where(g => g.CurrentStockQuantity > 0), "ItemCode", "ItemCode");
            return View(groundBalance);
        }

        private static List<GroundBalanceDetail> BuildPostedDetails(string batchNo, int[] detailIds, string[] itemCodes, float[] actualQty, float[] inventoryQty, string[] itemRemarks)
        {
            var details = new List<GroundBalanceDetail>();

            if (itemCodes == null)
            {
                return details;
            }

            for (int i = 0; i < itemCodes.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(itemCodes[i]))
                {
                    continue;
                }

                var actual = i < actualQty.Length ? actualQty[i] : 0;
                var inventory = i < inventoryQty.Length ? inventoryQty[i] : 0;

                details.Add(new GroundBalanceDetail
                {
                    Id = i < detailIds.Length ? detailIds[i] : 0,
                    BatchNo = batchNo,
                    ItemCode = itemCodes[i],
                    ActualQty = actual,
                    InventoryQty = inventory,
                    DifferentQty = actual - inventory,
                    Remark = i < itemRemarks.Length ? itemRemarks[i] ?? "" : ""
                });
            }

            return details;
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

        [HttpGet]
        public IActionResult GetSuppliers()
        {
            var suppliers = _context.Suppliers
                .OrderBy(s => s.Name)
                .Select(s => new { value = s.Id, text = s.Name })
                .ToList();
            return Json(suppliers);
        }

        [HttpGet]
        public IActionResult GetCategories(int? supplierId)
        {
            var query = _context.Categories.AsQueryable();
            
            if (supplierId.HasValue)
            {
                // Get categories that have products from the selected supplier
                var supplier = _context.Suppliers.Include(s => s.Products).FirstOrDefault(s => s.Id == supplierId);
                if (supplier != null)
                {
                    var categoryIds = supplier.Products.Select(p => p.CategoryId).Distinct().ToList();
                    query = query.Where(c => categoryIds.Contains(c.Id));
                }
            }
            
            var categories = query
                .OrderBy(c => c.Name)
                .Select(c => new { value = c.Id, text = c.Name })
                .ToList();
            return Json(categories);
        }

        [HttpGet]
        public IActionResult GetFilteredItems(int? supplierId, int? categoryId)
        {
            var query = _context.GRNDetails
                .Include(g => g.Product)
                .Where(g => g.CurrentStockQuantity > 0)
                .AsQueryable();
            
            if (supplierId.HasValue)
            {
                // Filter by supplier using the many-to-many relationship
                var supplier = _context.Suppliers.Include(s => s.Products).FirstOrDefault(s => s.Id == supplierId);
                if (supplier != null)
                {
                    var productIds = supplier.Products.Select(p => p.Id).ToList();
                    query = query.Where(g => productIds.Contains(g.ProductId));
                }
            }
            
            if (categoryId.HasValue)
            {
                query = query.Where(g => g.Product.CategoryId == categoryId);
            }
            
            var items = query
                .OrderBy(g => g.ItemCode)
                .Select(g => new { 
                    value = g.ItemCode, 
                    text = g.ItemCode + " - " + (g.Product != null ? g.Product.Name : "Unknown") 
                })
                .ToList();
            return Json(items);
        }
    }
}
