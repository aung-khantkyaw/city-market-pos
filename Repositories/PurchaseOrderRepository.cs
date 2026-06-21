using CityMarketPOS.Models;
using Microsoft.EntityFrameworkCore;

namespace CityMarketPOS.Repositories
{
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public PurchaseOrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PurchaseOrder>> GetAllPOAsync() => await _context.PurchaseOrders
                  .Include(po => po.Supplier) 
                  .ToListAsync();

        public async Task<PurchaseOrder> GetByIdWithDetailsAsync(int id)
            => await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.OrderDetails).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task AddAsync(PurchaseOrder purchaseOrder)
            => await _context.PurchaseOrders.AddAsync(purchaseOrder);

        public void UpdateStatus(int id, string status)
        {
            var po = _context.PurchaseOrders.Find(id);
            if (po != null) { po.Status = status; _context.Update(po); }
        }

        public async Task<bool> SaveChangesAsync() => (await _context.SaveChangesAsync()) > 0;
    }
}