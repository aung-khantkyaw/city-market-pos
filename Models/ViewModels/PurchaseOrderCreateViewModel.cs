using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CityMarketPOS.Models.ViewModels
{
    public class PurchaseOrderItemViewModel
    {
        [Required(ErrorMessage = "Please select a product.")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Please enter quantity.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
    }

    public class PurchaseOrderCreateViewModel
    {
        [Required]
        public string PONumber { get; set; }

        [Required(ErrorMessage = "Please select a supplier.")]
        public int SupplierId { get; set; }

        public List<PurchaseOrderItemViewModel> Items { get; set; } = new List<PurchaseOrderItemViewModel>();
    }
}