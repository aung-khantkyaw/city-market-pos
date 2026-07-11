using CityMarketPOS.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public class GRNRepository : IGRNRepository
    {
        private readonly ApplicationDbContext _context;

        public GRNRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<GRN>> GetAllAsync()
        {
            return await _context.GRNs
                .Include(g => g.PurchaseOrder)
                .Include(g => g.GRNDetails)
                .OrderByDescending(g => g.ReceivedDate)
                .ToListAsync();
        }

        public async Task<GRN> GetByIdAsync(int id)
        {
            return await _context.GRNs
                .Include(g => g.PurchaseOrder)
                .Include(g => g.GRNDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<GRN>> GetByPurchaseOrderIdAsync(int poId)
        {
            return await _context.GRNs
                .Include(g => g.PurchaseOrder)
                .Include(g => g.GRNDetails)
                .Where(g => g.PurchaseOrderId == poId)
                .OrderByDescending(g => g.ReceivedDate)
                .ToListAsync();
        }

        public async Task<PurchaseOrder> GetPurchaseOrderWithDetailsAsync(int poId) =>
        await _context.PurchaseOrders
            .Include(p => p.OrderDetails)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == poId);

        public async Task<Dictionary<int, int>> GetReceivedQuantitiesByPOAsync(int poId)
        {
            return await _context.GRNDetails
                .Where(gd => gd.GRN.PurchaseOrderId == poId)
                .GroupBy(gd => gd.ProductId)
                .Select(g => new { ProductId = g.Key, TotalReceived = g.Sum(x => x.ReceivedQuantity) })
                .ToDictionaryAsync(k => k.ProductId, v => v.TotalReceived);
        }

        public async Task ConfirmGRNAndUpdateStockAsync(GRN grn, List<int> productIds, List<int> receivedQtys, List<DateTime?> expiryDates, List<decimal> sellingPrices, int poId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var po = await _context.PurchaseOrders
                    .Include(p => p.Supplier)
                    .Include(p => p.OrderDetails)
                        .ThenInclude(od => od.Product)
                            .ThenInclude(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == poId);

                if (po == null) throw new Exception("Purchase Order not found.");

                int supplierId = po.SupplierId;

                grn.GRNDetails = new List<GRNDetail>();

                for (int i = 0; i < productIds.Count; i++)
                {
                    if (receivedQtys[i] > 0)
                    {
                        var poItem = po.OrderDetails.FirstOrDefault(od => od.ProductId == productIds[i]);

                        if (poItem == null) continue;

                        decimal pPrice = poItem.UnitPrice;
                        string categoryShortName = poItem.Product?.Category?.ShortName ?? "UNK";
                        int productId = productIds[i];
                        string date = DateTime.Now.ToString("yyMMddHHmm");

                        string generatedItemCode = $"{categoryShortName}{productId}{supplierId}{date}";

                        grn.GRNDetails.Add(new GRNDetail
                        {
                            ProductId = productId,
                            ReceivedQuantity = receivedQtys[i],
                            StockQuantity = receivedQtys[i],
                            CurrentStockQuantity = receivedQtys[i],

                            ExpiryDate = expiryDates[i],
                            SellingPrice = sellingPrices[i],
                            PurchasePrice = pPrice,
                            ItemCode = generatedItemCode
                        });
                    }
                }

                _context.GRNs.Add(grn);
                await _context.SaveChangesAsync();

                bool isFullyReceived = true;
                var allReceivedQtys = await GetReceivedQuantitiesByPOAsync(poId);

                foreach (var poDetail in po.OrderDetails)
                {
                    allReceivedQtys.TryGetValue(poDetail.ProductId, out int totalReceived);
                    if (totalReceived < poDetail.Quantity)
                    {
                        isFullyReceived = false;
                        break;
                    }
                }

                po.Status = isFullyReceived ? "Received" : "Partially Received";
                _context.Update(po);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            return await _context.Products
                .Where(p => _context.GRNDetails
                    .Where(g => g.ProductId == p.Id)
                    .Sum(g => g.CurrentStockQuantity) <= p.MinStockLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<GRNDetail>> GetExpiringItemsAsync(int daysThreshold)
        {
            var targetDate = DateTime.Now.AddDays(daysThreshold);
            return await _context.GRNDetails
                .Include(g => g.Product)
                .Include(g => g.GRN)
                .Where(g => g.CurrentStockQuantity > 0 && g.ExpiryDate.HasValue && g.ExpiryDate.Value <= targetDate)
                .OrderBy(g => g.ExpiryDate)
                .ToListAsync();
        }
    }
}