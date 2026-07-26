using CityMarketPOS.Models;
using System.Collections.Generic;

namespace CityMarketPOS.Models.ViewModels
{
    public class StockGroupViewModel
    {
        public int POId { get; set; }
        public string PONumber { get; set; }
        public System.DateTime OrderDate { get; set; }
        public string Supplier { get; set; }
        public List<GRNDetail> Details { get; set; }
    }
}
