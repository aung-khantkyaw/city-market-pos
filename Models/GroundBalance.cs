using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityMarketPOS.Models
{
    public class GroundBalance
    {
        [Key]
        [StringLength(25)]
        public string BatchNo { get; set; } // e.g., GB-20260721-001

        public DateTime GBDate { get; set; }

        [Required]
        [StringLength(10)]
        public string Status { get; set; } // Pending, Completed, Cancelled

        [StringLength(100)]
        public string Remark { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string CreatedUser { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(50)]
        public string UpdatedUser { get; set; }

        public List<GroundBalanceDetail> GroundBalanceDetails { get; set; } = new List<GroundBalanceDetail>();
    }
}
