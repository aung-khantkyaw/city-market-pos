using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityMarketPOS.Models
{
    public class GroundBalanceDetail
    {
        [Key]
        public int Id { get; set; }

        [StringLength(25)]
        public string BatchNo { get; set; }

        [ForeignKey("BatchNo")]
        public GroundBalance GroundBalance { get; set; }

        [Required]
        [StringLength(25)]
        public string ItemCode { get; set; } // GRN မှ ထုတ်ပေးထားသော Item Code

        public float ActualQty { get; set; } // မြေပြင်တွင် တကယ်ရှိသော အရေအတွက်

        public float InventoryQty { get; set; } // System တွင် မှတ်ထားသော အရေအတွက်

        public float DifferentQty { get; set; } // ကွာဟချက် (ActualQty - InventoryQty)

        [StringLength(100)]
        public string Remark { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string CreatedUser { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(50)]
        public string UpdatedUser { get; set; }
    }
}
