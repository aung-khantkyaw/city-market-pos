using CityMarketPOS.Models;
using System.Collections.Generic;
using System.Linq;

namespace CityMarketPOS.Models.ViewModels
{
    public class SessionReportViewModel
    {
        public POSSession Session { get; set; }
        public List<Sale> Sales { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }
    }
}
