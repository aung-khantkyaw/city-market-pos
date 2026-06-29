using CityMarketPOS.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public class BatchTrackingRepository : IBatchTrackingRepository
    {
        private readonly ApplicationDbContext _context;

        public BatchTrackingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<GRNDetail>> GetActiveBatchesAsync()
        {
            return await _context.GRNDetails
                .Include(d => d.Product)
                .Include(d => d.GRN)
                .Where(d => (d.CurrentStoreQuantity + d.CurrentStockQuantity) > 0)
                .OrderBy(d => d.ExpiryDate ?? DateTime.MaxValue) 
                .ToListAsync();
        }

        public async Task<GRNDetail> GetBatchDetailByIdAsync(int detailId)
        {
            return await _context.GRNDetails.FindAsync(detailId);
        }

        public async Task<(bool Success, string Message)> TransferToStockAsync(int detailId, int qty)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var batchDetail = await _context.GRNDetails.FindAsync(detailId);
                if (batchDetail == null) return (false, "Batch not found.");
                if (batchDetail.CurrentStoreQuantity < qty) return (false, "Not enough stock.");

                batchDetail.CurrentStoreQuantity -= qty;
                batchDetail.CurrentStockQuantity += qty;
                _context.Update(batchDetail);

                var log = new StockTransferLog
                {
                    GRNDetailId = detailId,
                    TransferQuantity = qty,
                    TransferDate = DateTime.Now
                };
                _context.StockTransferLogs.Add(log);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Transferred successfully.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return (false, "Error occurred.");
            }
        }
    }
}