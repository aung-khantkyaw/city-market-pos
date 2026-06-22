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

        public async Task<GRN> GetByPurchaseOrderIdAsync(int poId)
        {
            return await _context.GRNs
                .FirstOrDefaultAsync(g => g.PurchaseOrderId == poId);
        }

        public async Task<PurchaseOrder> GetPurchaseOrderWithDetailsAsync(int poId) =>
            await _context.PurchaseOrders
                .Include(p => p.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(p => p.Id == poId);

        public async Task ConfirmGRNAndUpdateStockAsync(GRN grn, List<int> productIds, List<int> receivedQtys, List<DateTime?> expiryDates, PurchaseOrder po)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                grn.GRNDetails = new List<GRNDetail>();

                for (int i = 0; i < productIds.Count; i++)
                {
                    if (receivedQtys[i] > 0)
                    {
                        grn.GRNDetails.Add(new GRNDetail
                        {
                            ProductId = productIds[i],
                            ReceivedQuantity = receivedQtys[i],
                            CurrentQuantity = receivedQtys[i],
                            ExpiryDate = expiryDates[i]
                        });

                        var product = await _context.Products.FindAsync(productIds[i]);
                        if (product != null)
                        {
                            product.StockQuantity += receivedQtys[i];
                            //_context.Products.Update(product);
                        }
                    }
                }

                po.Status = "Received";
                _context.GRNs.Add(grn);

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
            var products = await _context.Products.ToListAsync();
            return products.Where(p => p.TotalQuantity <= p.MinStockLevel).ToList();
        }

        public async Task<IEnumerable<GRNDetail>> GetExpiringItemsAsync(int daysThreshold)
        {
            var targetDate = DateTime.Now.AddDays(daysThreshold);
            return await _context.GRNDetails
                .Include(g => g.Product)
                .Include(g => g.GRN)
                .Where(g => g.CurrentQuantity > 0 && g.ExpiryDate.HasValue && g.ExpiryDate.Value <= targetDate)
                .OrderBy(g => g.ExpiryDate)
                .ToListAsync();
        }
    }
}