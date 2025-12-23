namespace TechKingPOS.App.Models
{
    public class ItemLookup
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;

        public int Quantity { get; set; }          // ✅ REQUIRED
        public int? TargetQuantity { get; set; }   // ✅ NULLABLE TARGET

        public decimal MarkedPrice { get; set; }
        public decimal SellingPrice { get; set; }

        public string Unit { get; set; } = string.Empty;
        public int Deficit =>
            TargetQuantity.HasValue
                ? Math.Max(0, TargetQuantity.Value - Quantity)
                : 0;
        // Used by Sales list
        public string Display => $"{Name} ({Unit}) - {SellingPrice:C}";
    }
}
