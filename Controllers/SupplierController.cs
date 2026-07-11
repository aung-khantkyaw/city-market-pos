using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
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

        public SupplierController(ISupplierRepository supplierRepo, IProductRepository productRepo, ApplicationDbContext context)
        {
            _supplierRepo = supplierRepo;
            _productRepo = productRepo;
            _context = context;
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

            if (ModelState.IsValid)
            {
                if (phoneNumbers != null && phoneNumbers.Any())
                {
                    supplier.Phone = string.Join(", ", phoneNumbers.Where(p => !string.IsNullOrWhiteSpace(p)));
                }

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
            var supplier = await _supplierRepo.GetByIdAsync(id);
            if (supplier != null)
            {
                _supplierRepo.Delete(supplier);
                await _supplierRepo.SaveChangesAsync();
                TempData["Success"] = "Supplier deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}