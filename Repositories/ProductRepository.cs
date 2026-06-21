using CityMarketPOS.Models;
using Microsoft.EntityFrameworkCore;

namespace CityMarketPOS.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Product>> GetAllAsync() =>
            await _context.Products.Include(p => p.Category).Include(p => p.Brand).Include(p => p.UOM).ToListAsync();

        public async Task<Product> GetByIdAsync(int id) => await _context.Products.FindAsync(id);

        public async Task AddAsync(Product product) => await _context.Products.AddAsync(product);

        public void Update(Product product) => _context.Products.Update(product);

        public void Delete(Product product) => _context.Products.Remove(product);

        public async Task<bool> SaveChangesAsync() => (await _context.SaveChangesAsync()) > 0;

        public string GenerateBarcode()
        {
            var random = new Random();
            string timestamp = DateTime.Now.ToString("yyMMddHHmm");
            string randomNumber = random.Next(100, 999).ToString();
            return $"CM-{timestamp}-{randomNumber}";
        }
    }
}