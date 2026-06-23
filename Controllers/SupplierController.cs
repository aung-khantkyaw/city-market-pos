using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager,Inventory")]
    public class SupplierController : Controller
    {
        private readonly ISupplierRepository _supplierRepo;
        private readonly IProductRepository _productRepo;

        public SupplierController(ISupplierRepository supplierRepo, IProductRepository productRepo)
        {
            _supplierRepo = supplierRepo;
            _productRepo = productRepo;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _supplierRepo.GetAllAsync());
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Products = new MultiSelectList(await _productRepo.GetAllAsync(), "Id", "Name");
            return PartialView("_CreatePartial");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier, int[] selectedProductIds)
        {
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
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

            ViewBag.Products = new MultiSelectList(await _productRepo.GetAllAsync(), "Id", "Name", selectedProductIds);
            return PartialView("_CreatePartial", supplier); 
        }

        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _supplierRepo.GetByIdAsync(id);
            if (supplier == null) return NotFound();

            var selectedProductIds = supplier.Products.Select(p => p.Id).ToArray();
            ViewBag.Products = new MultiSelectList(await _productRepo.GetAllAsync(), "Id", "Name", selectedProductIds);

            return PartialView("_EditPartial", supplier); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier, int[] selectedProductIds)
        {
            if (id != supplier.Id) return NotFound();

            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                var existingSupplier = await _supplierRepo.GetByIdAsync(id);
                if (existingSupplier == null) return NotFound();

                existingSupplier.Name = supplier.Name;
                existingSupplier.ContactPerson = supplier.ContactPerson;
                existingSupplier.Phone = supplier.Phone;
                existingSupplier.Email = supplier.Email;
                existingSupplier.Address = supplier.Address;

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

            ViewBag.Products = new MultiSelectList(await _productRepo.GetAllAsync(), "Id", "Name", selectedProductIds);
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