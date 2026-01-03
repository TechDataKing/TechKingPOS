namespace TechKingPOS.App.Models
{
    public class ItemLookup
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;

        public decimal Quantity { get; set; }          // ✅ REQUIRED
        public decimal? TargetQuantity { get; set; }   // ✅ NULLABLE TARGET

        public decimal MarkedPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int BranchId { get; set; }


        public string UnitType { get; set; } = string.Empty;
        public decimal Deficit =>
            TargetQuantity.HasValue
                ? Math.Max(0, TargetQuantity.Value - Quantity)
                : 0;
        // Used by Sales list
        public string Display => $"{Name} ({UnitType}) - {SellingPrice:C}";
    }
}
