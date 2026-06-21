using CityMarketPOS.Models;

namespace CityMarketPOS.Repositories
{
    public interface IGRNRepository
    {
        Task CreateGRNAsync(GRN grn, int poId);
    }
}
