using CityMarketPOS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityMarketPOS.Repositories
{
    public interface IPurchaseOrderRepository
    {
        Task<IEnumerable<PurchaseOrder>> GetAllPOAsync();
        Task<PurchaseOrder> GetByIdWithDetailsAsync(int id);
        Task AddAsync(PurchaseOrder purchaseOrder);
        void UpdateStatus(int id, string status);
        Task<bool> SaveChangesAsync();
    }
}