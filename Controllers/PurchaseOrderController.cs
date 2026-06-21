using CityMarketPOS.Models;
using CityMarketPOS.Models.ViewModels;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CityMarketPOS.Controllers
{
    public class PurchaseOrderController : Controller
    {
        private readonly IPurchaseOrderRepository _poRepo;
        private readonly ApplicationDbContext _context;

        public PurchaseOrderController(IPurchaseOrderRepository poRepo, ApplicationDbContext context)
        {
            _poRepo = poRepo;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var pos = await _poRepo.GetAllPOAsync(); 
            return View(pos);
        }
        public IActionResult Create()
        {
            var model = new PurchaseOrderViewModel
            {
                PONumber = "PO-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                OrderDetails = new List<PurchaseOrderDetailViewModel> { new PurchaseOrderDetailViewModel() }
            };

            ViewData["Suppliers"] = new SelectList(_context.Suppliers, "Id", "Name");
            ViewBag.ProductList = _context.Products.Select(p => new { p.Id, p.Name, p.PurchasePrice }).ToList();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PurchaseOrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Suppliers"] = new SelectList(_context.Suppliers, "Id", "Name", model.SupplierId);
                ViewBag.ProductList = _context.Products.Select(p => new { p.Id, p.Name, p.PurchasePrice }).ToList();
                return View(model);
            }

            var po = new PurchaseOrder
            {
                PONumber = model.PONumber, 
                SupplierId = model.SupplierId,
                Status = "Pending",
                OrderDate = DateTime.Now
            };

            po.OrderDetails = model.OrderDetails.Select(d => new PurchaseOrderDetail
            {
                ProductId = d.ProductId,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                TotalPrice = d.Quantity * d.UnitPrice
            }).ToList();

            po.TotalAmount = po.OrderDetails.Sum(x => x.TotalPrice);

            _context.PurchaseOrders.Add(po);
            await _context.SaveChangesAsync(); 

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Details(int id)
        {
            var po = await _poRepo.GetByIdWithDetailsAsync(id);

            if (po == null)
            {
                return NotFound(); 
            }

            return View(po);
        }

        [HttpGet]
        public IActionResult GetProductPrice(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            return Content(product.PurchasePrice.ToString());
        }
    }
}