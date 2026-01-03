using System;

namespace TechKingPOS.App.Models
{
    public class RepackRuleModel
    {
        public int Id { get; set; }

        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int BranchId { get; set; }


        public decimal Quantity { get; set; }
        public string UnitType { get; set; }
        public decimal UnitValue { get; set; }

        public decimal SellingPrice { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public string Display =>
    $"{UnitValue} {UnitType} @ {SellingPrice:0.00}";

    }
}
