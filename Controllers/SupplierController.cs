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
    public class SupplierController : Controller
    {
        private readonly ISupplierRepository _supplierRepo;
        private readonly IProductRepository _productRepo;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public SupplierController(ISupplierRepository supplierRepo, IProductRepository productRepo, ApplicationDbContext context, IAuditLogRepository auditLogRepo, UserManager<ApplicationUser> userManager)
        {
            _supplierRepo = supplierRepo;
            _productRepo = productRepo;
            _context = context;
            _auditLogRepo = auditLogRepo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _supplierRepo.GetAllAsync());
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Products = await _productRepo.GetAllAsync();
            ViewBag.SelectedProducts = new int[0]; 

            return PartialView("_CreatePartial");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier, int[] selectedProductIds, List<string> phoneNumbers)
        {
            ModelState.Remove("Products");
            ModelState.Remove("Phone");

            var submittedPhones = phoneNumbers?
                .Where(phone => !string.IsNullOrWhiteSpace(phone))
                .Select(phone => phone.Trim())
                .ToList() ?? new List<string>();
            var submittedEmail = supplier.Email?.Trim();

            if (submittedPhones.Any() || !string.IsNullOrWhiteSpace(submittedEmail))
            {
                var existingSuppliers = await _context.Suppliers
                    .AsNoTracking()
                    .Select(existing => new { existing.Phone, existing.Email })
                    .ToListAsync();

                var existingPhones = existingSuppliers
                    .Where(existing => !string.IsNullOrWhiteSpace(existing.Phone))
                    .SelectMany(existing => existing.Phone.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    .Select(phone => phone.Trim());

                if (submittedPhones.Intersect(existingPhones, StringComparer.OrdinalIgnoreCase).Any())
                {
                    ModelState.AddModelError("phoneNumbers", "This phone number is already registered to another supplier.");
                }

                var existingEmails = existingSuppliers
                    .Where(existing => !string.IsNullOrWhiteSpace(existing.Email))
                    .SelectMany(existing => existing.Email.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    .Select(email => email.Trim());

                if (!string.IsNullOrWhiteSpace(submittedEmail) &&
                    existingEmails.Contains(submittedEmail, StringComparer.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("Email", "This email address is already registered to another supplier.");
                }
            }

            if (ModelState.IsValid)
            {
                if (submittedPhones.Any())
                {
                    supplier.Phone = string.Join(", ", submittedPhones);
                }

                supplier.Email = submittedEmail;

                if (selectedProductIds != null)
                {
                    foreach (var id in selectedProductIds)
                    {
                        var product = await _productRepo.GetByIdAsync(id);
                        if (product != null)
                        {
                            supplier.Products.Add(product);
                        }
                    }
                }

                await _supplierRepo.AddAsync(supplier);
                await _supplierRepo.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                await _auditLogRepo.LogAsync("Supplier", supplier.Id.ToString(), "Create", user?.Id ?? "System", user?.UserName ?? "System", $"Created supplier: {supplier.Name}");

                TempData["Success"] = "Supplier added successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Products = await _productRepo.GetAllAsync();
            ViewBag.SelectedProducts = selectedProductIds ?? new int[0];

            return PartialView("_CreatePartial", supplier);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _supplierRepo.GetByIdAsync(id);
            if (supplier == null) return NotFound();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Products = await _productRepo.GetAllAsync();
            ViewBag.SelectedProducts = supplier.Products.Select(p => p.Id).ToArray();

            return PartialView("_EditPartial", supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier, int[] selectedProductIds, List<string> phoneNumbers)
        {
            if (id != supplier.Id) return NotFound();

            ModelState.Remove("Products");
            ModelState.Remove("Phone");

            if (ModelState.IsValid)
            {
                var existingSupplier = await _supplierRepo.GetByIdAsync(id);
                if (existingSupplier == null) return NotFound();

                var oldName = existingSupplier.Name;
                existingSupplier.Name = supplier.Name;
                existingSupplier.ContactPerson = supplier.ContactPerson;
                existingSupplier.Email = supplier.Email;
                existingSupplier.Address = supplier.Address;

                if (phoneNumbers != null && phoneNumbers.Any())
                {
                    existingSupplier.Phone = string.Join(", ", phoneNumbers.Where(p => !string.IsNullOrWhiteSpace(p)));
                }
                else
                {
                    existingSupplier.Phone = null;
                }

                existingSupplier.Products.Clear();

                if (selectedProductIds != null)
                {
                    foreach (var prodId in selectedProductIds)
                    {
                        var product = await _productRepo.GetByIdAsync(prodId);
                        if (product != null)
                        {
                            existingSupplier.Products.Add(product);
                        }
                    }
                }

                _supplierRepo.Update(existingSupplier);
                await _supplierRepo.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                await _auditLogRepo.LogAsync("Supplier", supplier.Id.ToString(), "Update", user?.Id ?? "System", user?.UserName ?? "System", $"Updated supplier: {supplier.Name}", oldValues: $"Old: {oldName}", newValues: $"New: {supplier.Name}");

                TempData["Success"] = "Supplier updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Products = await _productRepo.GetAllAsync();
            ViewBag.SelectedProducts = selectedProductIds ?? new int[0];

            return PartialView("_EditPartial", supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.PurchaseOrders)
                    .ThenInclude(po => po.OrderDetails)
                .Include(s => s.PurchaseOrders)
                    .ThenInclude(po => po.GRNs)
                    .ThenInclude(g => g.GRNDetails)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier != null)
            {
                var user = await _userManager.GetUserAsync(User);
                var supplierName = supplier.Name;

                // Cascade delete: GRN/GRN Details -> PO/PO Details -> Supplier
                foreach (var po in supplier.PurchaseOrders.ToList())
                {
                    // Delete GRN Details first
                    foreach (var grn in po.GRNs.ToList())
                    {
                        var grnDetails = await _context.GRNDetails.Where(g => g.GRNId == grn.Id).ToListAsync();
                        _context.GRNDetails.RemoveRange(grnDetails);
                    }

                    // Delete GRNs
                    _context.GRNs.RemoveRange(po.GRNs);

                    // Delete PO Details
                    _context.PurchaseOrderDetails.RemoveRange(po.OrderDetails);
                }

                // Delete POs
                _context.PurchaseOrders.RemoveRange(supplier.PurchaseOrders);

                // Delete Supplier
                _context.Suppliers.Remove(supplier);

                await _context.SaveChangesAsync();

                await _auditLogRepo.LogAsync("Supplier", id.ToString(), "Delete", user?.Id ?? "System", user?.UserName ?? "System", $"Deleted supplier: {supplierName} with cascade delete of related POs and GRNs");

                TempData["Success"] = "Supplier and all related data deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
