using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Cashier")]
    public class POSTerminalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogRepository _auditLogRepo;

        public POSTerminalController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditLogRepository auditLogRepo)
        {
            _context = context;
            _userManager = userManager;
            _auditLogRepo = auditLogRepo;
        }

        private async Task<POSSession> GetCurrentSession()
        {
            var sessionId = HttpContext.Session.GetInt32("POSSessionId");
            if (sessionId == null) return null;

            return await _context.POSSessions
                .Include(s => s.Counter)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.Status == "Active");
        }

        public async Task<IActionResult> Index()
        {
            var session = await GetCurrentSession();
            if (session == null)
            {
                return RedirectToAction("SelectCounter", "POSSession");
            }

            ViewBag.Session = session;
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.UOM)
                .OrderBy(p => p.Name)
                .ToListAsync();
            ViewBag.StockSummaries = await _context.GRNDetails
                .Where(g => g.CurrentStockQuantity > 0)
                .GroupBy(g => g.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Stock = g.Sum(x => x.CurrentStockQuantity),
                    Price = g.Min(x => x.SellingPrice)
                })
                .ToDictionaryAsync(x => x.ProductId, x => new ProductStockSummary { Stock = x.Stock, Price = x.Price });

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SearchProducts(string searchTerm, int? categoryId = null)
        {
            var session = await GetCurrentSession();
            if (session == null)
            {
                return Json(new { success = false, message = "No active session" });
            }

            var query = _context.Products.AsQueryable();

            // Search by ItemCode (from GRNDetail) or product name
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => 
                    p.Name.Contains(searchTerm) ||
                    _context.GRNDetails.Any(g => g.ProductId == p.Id && g.ItemCode != null && g.ItemCode.Contains(searchTerm))
                );
            }

            // Filter by category
            if (categoryId.HasValue && categoryId.Value >= 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var products = await query
                .Include(p => p.Category)
                .Include(p => p.UOM)
                .OrderBy(p => p.Name)
                .Take(20)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    Category = p.Category == null ? null : new { p.Category.Id, p.Category.Name },
                    Uom = p.UOM == null ? null : new { p.UOM.Id, p.UOM.ShortName },
                    AvailableStock = _context.GRNDetails
                        .Where(g => g.ProductId == p.Id && g.CurrentStockQuantity > 0)
                        .Sum(g => (int?)g.CurrentStockQuantity) ?? 0,
                    StartingPrice = _context.GRNDetails
                        .Where(g => g.ProductId == p.Id && g.CurrentStockQuantity > 0)
                        .Min(g => (decimal?)g.SellingPrice) ?? 0
                })
                .ToListAsync();

            return Json(new { success = true, products });
        }

        [HttpPost]
        public async Task<IActionResult> GetProductStock(int productId)
        {
            var stockDetails = await _context.GRNDetails
                .Include(g => g.Product)
                .Where(g => g.ProductId == productId && g.CurrentStockQuantity > 0)
                .OrderBy(g => g.ExpiryDate)
                .ToListAsync();

            return Json(new { success = true, stockDetails });
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int grnDetailId, int quantity)
        {
            var session = await GetCurrentSession();
            if (session == null)
            {
                return Json(new { success = false, message = "No active session" });
            }

            var grnDetail = await _context.GRNDetails
                .Include(g => g.Product)
                .FirstOrDefaultAsync(g => g.Id == grnDetailId);

            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Quantity must be greater than zero" });
            }

            if (grnDetail == null)
            {
                return Json(new { success = false, message = "Stock batch was not found" });
            }

            if (grnDetail.ProductId != productId)
            {
                return Json(new { success = false, message = "Selected stock does not match the product" });
            }

            if (grnDetail.CurrentStockQuantity < quantity)
            {
                return Json(new { success = false, message = "Insufficient stock" });
            }

            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            
            var existingItem = cart.FirstOrDefault(c => c.GRNDetailId == grnDetailId);
            if (existingItem != null)
            {
                if (existingItem.Quantity + quantity > grnDetail.CurrentStockQuantity)
                {
                    return Json(new { success = false, message = $"Only {grnDetail.CurrentStockQuantity} units are available for this batch" });
                }

                existingItem.Quantity += quantity;
                existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = productId,
                    GRNDetailId = grnDetailId,
                    ProductName = grnDetail.Product.Name,
                    Quantity = quantity,
                    UnitPrice = grnDetail.SellingPrice,
                    TotalPrice = quantity * grnDetail.SellingPrice
                });
            }

            HttpContext.Session.Set("Cart", cart);

            return Json(new { success = true, cart });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCart(int grnDetailId, int quantity)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            
            var item = cart.FirstOrDefault(c => c.GRNDetailId == grnDetailId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    var availableStock = await _context.GRNDetails
                        .Where(g => g.Id == grnDetailId)
                        .Select(g => g.CurrentStockQuantity)
                        .FirstOrDefaultAsync();

                    if (availableStock <= 0)
                    {
                        cart.Remove(item);
                        HttpContext.Session.Set("Cart", cart);
                        return Json(new { success = false, message = "This stock batch is no longer available" });
                    }

                    if (quantity > availableStock)
                    {
                        return Json(new { success = false, message = $"Only {availableStock} units are available for this batch" });
                    }

                    item.Quantity = quantity;
                    item.TotalPrice = item.Quantity * item.UnitPrice;
                }
            }

            HttpContext.Session.Set("Cart", cart);
            return Json(new { success = true, cart });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int grnDetailId)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            
            var item = cart.FirstOrDefault(c => c.GRNDetailId == grnDetailId);
            if (item != null)
            {
                cart.Remove(item);
            }

            HttpContext.Session.Set("Cart", cart);
            return Json(new { success = true, cart });
        }

        [HttpPost]
        public IActionResult GetCart()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            var subtotal = cart.Sum(c => c.TotalPrice);
            var tax = subtotal * 0.05m; // 5% tax
            var total = subtotal + tax;
            var itemCount = cart.Sum(c => c.Quantity);

            return Json(new { success = true, cart, subtotal, tax, total, itemCount });
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove("Cart");
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CompleteSale(string paymentMethod, string? cardNumber = null, decimal? cashReceived = null, string? paymentReference = null)
        {
            var session = await GetCurrentSession();
            if (session == null)
            {
                return Json(new { success = false, message = "No active session" });
            }

            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (!cart.Any())
            {
                return Json(new { success = false, message = "Cart is empty" });
            }

            var subtotal = cart.Sum(c => c.TotalPrice);
            var tax = subtotal * 0.05m; // 5% tax
            var total = subtotal + tax;
            var cashTendered = total;
            var changeDue = 0m;

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                return Json(new { success = false, message = "Payment method is required" });
            }

            if (paymentMethod == "Cash")
            {
                if (!cashReceived.HasValue || cashReceived.Value < total)
                {
                    return Json(new { success = false, message = $"Cash received must be at least {total:F2}" });
                }

                cashTendered = cashReceived.GetValueOrDefault();
                changeDue = cashTendered - total;
            }
            else if (paymentMethod == "Card" && string.IsNullOrWhiteSpace(cardNumber))
            {
                return Json(new { success = false, message = "Please enter card number" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // Create sale
            var saleNumber = "SALE-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var sale = new Sale
            {
                SaleNumber = saleNumber,
                SaleDate = DateTime.Now,
                POSSessionId = session.Id,
                CashierId = session.UserId,
                CashierName = session.UserName,
                CounterId = session.CounterId,
                Subtotal = subtotal,
                Tax = tax,
                Total = total,
                Discount = 0,
                GrandTotal = total,
                PaymentMethod = paymentMethod,
                CardNumber = paymentMethod == "Card" ? MaskCardNumber(cardNumber) : paymentReference,
                Status = "Completed",
                Notes = paymentMethod == "Cash" ? $"Cash received: {cashTendered:F2}; Change due: {changeDue:F2}" : paymentReference
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            // Create sale details and update stock
            foreach (var cartItem in cart)
            {
                var grnDetail = await _context.GRNDetails
                    .FirstOrDefaultAsync(g => g.Id == cartItem.GRNDetailId);

                if (grnDetail == null)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = $"{cartItem.ProductName} stock batch was not found. Please remove it from the cart and try again." });
                }

                if (grnDetail.CurrentStockQuantity < cartItem.Quantity)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = $"{cartItem.ProductName} only has {grnDetail.CurrentStockQuantity} units available. Please update the cart." });
                }

                grnDetail.CurrentStockQuantity -= cartItem.Quantity;
                _context.GRNDetails.Update(grnDetail);

                var saleDetail = new SaleDetail
                {
                    SaleId = sale.Id,
                    ProductId = cartItem.ProductId,
                    GRNDetailId = cartItem.GRNDetailId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice,
                    TotalPrice = cartItem.TotalPrice,
                    Discount = 0,
                    LineTotal = cartItem.TotalPrice
                };

                _context.SaleDetails.Add(saleDetail);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Clear cart
            HttpContext.Session.Remove("Cart");

            var user = await _userManager.GetUserAsync(User);
            await _auditLogRepo.LogAsync("Sale", sale.Id.ToString(), "Create", user?.Id ?? "System", user?.UserName ?? "System", $"Completed sale: {saleNumber}, Total: {total:F2}");

            return Json(new
            {
                success = true,
                saleId = sale.Id,
                saleNumber,
                cashier = session.UserName,
                counter = session.Counter?.Name,
                saleDate = sale.SaleDate.ToString("dd MMM yyyy HH:mm"),
                subtotal,
                tax,
                total,
                paymentMethod,
                cashReceived = cashTendered,
                changeDue,
                items = cart.Select(c => new
                {
                    c.ProductName,
                    c.Quantity,
                    c.UnitPrice,
                    c.TotalPrice
                })
            });
        }

        private static string? MaskCardNumber(string? cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
            {
                return null;
            }

            var digits = new string(cardNumber.Where(char.IsDigit).ToArray());
            if (digits.Length <= 4)
            {
                return "****";
            }

            return "**** **** **** " + digits[^4..];
        }

        public async Task<IActionResult> TodaySales()
        {
            var session = await GetCurrentSession();
            if (session == null)
            {
                return RedirectToAction("SelectCounter", "POSSession");
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var sales = await _context.Sales
                .Include(s => s.Details)
                .Include(s => s.POSSession)
                    .ThenInclude(p => p.Counter)
                .Where(s => s.CashierId == session.UserId && 
                           s.SaleDate >= today && 
                           s.SaleDate < tomorrow)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            var totalSales = sales.Count;
            var totalAmount = sales.Sum(s => s.GrandTotal);
            var totalItems = sales.Sum(s => s.Details.Sum(d => d.Quantity));
            var totalTax = sales.Sum(s => s.Tax);
            var totalDiscount = sales.Sum(s => s.Discount);

            ViewBag.Session = session;
            ViewBag.Sales = sales;
            ViewBag.TotalSales = totalSales;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalTax = totalTax;
            ViewBag.TotalDiscount = totalDiscount;
            ViewBag.Today = today;

            return View();
        }
    }

    public class CartItem
    {
        public int ProductId { get; set; }
        public int GRNDetailId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class ProductStockSummary
    {
        public int Stock { get; set; }
        public decimal Price { get; set; }
    }

    public static class SessionExtensions
    {
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, System.Text.Json.JsonSerializer.Serialize(value));
        }

        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : System.Text.Json.JsonSerializer.Deserialize<T>(value);
        }
    }
}
