using CityMarketPOS.Models;
using Microsoft.EntityFrameworkCore;

namespace CityMarketPOS.Repositories
{
    public class GRNRepository : IGRNRepository
    {
        private readonly ApplicationDbContext _context;
        public GRNRepository(ApplicationDbContext context) => _context = context;

        public async Task CreateGRNAsync(GRN grn, int poId)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.OrderDetails)
                .FirstOrDefaultAsync(p => p.Id == poId);

            if (po != null)
            {
                foreach (var item in po.OrderDetails)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity; 
                    }
                }

                po.Status = "Received";

                _context.GRNs.Add(grn);
                await _context.SaveChangesAsync();
            }
        }
    }
}
