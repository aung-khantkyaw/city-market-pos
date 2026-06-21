using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CityMarketPOS.Controllers
{
    public class GRNController : Controller
    {
        private readonly IGRNRepository _grnRepo;
        private readonly ApplicationDbContext _context;

        public GRNController(IGRNRepository grnRepo, ApplicationDbContext context)
        {
            _grnRepo = grnRepo;
            _context = context;
        }

        public IActionResult Create(int poId)
        {
            var po = _context.PurchaseOrders.Include(p => p.OrderDetails).ThenInclude(d => d.Product).FirstOrDefault(p => p.Id == poId);
            return View(po);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmGRN(int poId, string remarks)
        {
            var grn = new GRN { PurchaseOrderId = poId, GRNNumber = "GRN-" + DateTime.Now.Ticks, Remarks = remarks };
            await _grnRepo.CreateGRNAsync(grn, poId);
            return RedirectToAction("Index", "PurchaseOrder");
        }
    }
}
