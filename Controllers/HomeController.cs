using CityMarketPOS.Models.ViewModels;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CityMarketPOS.Controllers
{
    public class HomeController : Controller
    {
        //public IActionResult Index()
        //{
        //    return View();
        //}

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private readonly IGRNRepository _grnRepo;

        public HomeController(IGRNRepository grnRepo)
        {
            _grnRepo = grnRepo;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.LowStock = await _grnRepo.GetLowStockProductsAsync();
            ViewBag.Expiring = await _grnRepo.GetExpiringItemsAsync(7);

            return View();
        }
    }
}
