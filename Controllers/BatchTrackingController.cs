using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager,Inventory")]
    public class BatchTrackingController : Controller
    {
        private readonly IBatchTrackingRepository _batchRepo;
        private readonly ApplicationDbContext _context;

        public BatchTrackingController(IBatchTrackingRepository batchRepo, ApplicationDbContext context)
        {
            _batchRepo = batchRepo;
            _context = context;
        }

        public async Task<IActionResult> TransferHistory() 
        {
            var logs = await _context.StockTransferLogs
                .Include(l => l.GRNDetail)
                    .ThenInclude(d => d.Product) 
                .OrderByDescending(l => l.TransferDate)
                .ToListAsync();

            return View(logs);
        }

        public async Task<IActionResult> Index()
        {
            var activeBatches = await _batchRepo.GetActiveBatchesAsync();
            return View(activeBatches);
        }

        [HttpPost]
        public async Task<IActionResult> TransferStock(int detailId, int transferQty)
        {
            if (transferQty <= 0)
                return Json(new { success = false, message = "Transfer quantity must be greater than zero." });

            var result = await _batchRepo.TransferToStockAsync(detailId, transferQty);

            if (result.Success)
            {
                var updatedBatch = await _batchRepo.GetBatchDetailByIdAsync(detailId);
                return Json(new
                {
                    success = true,
                    message = result.Message,
                    newStoreQty = updatedBatch.CurrentStoreQuantity,
                    newStockQty = updatedBatch.CurrentStockQuantity
                });
            }

            return Json(new { success = false, message = result.Message });
        }
    }
}