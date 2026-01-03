using System;

namespace TechKingPOS.App.Models
{
    public class DamagedItem
    {
        public int Id { get; set; }

        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public string UnitType { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal MarkedPrice { get; set; }
        public decimal SellingPrice { get; set; }

        public string Reason { get; set; } = string.Empty;
        public string RecordedBy { get; set; } = string.Empty;

        public DateTime DamagedAt { get; set; }
        public int BranchId { get; set; }

    }
}
