namespace TechKingPOS.App.Models
{
    public class SaleItem
    {
        public int ItemId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;   // 🔹 ADD THIS
        public string UnitType { get; set; } = string.Empty;
        public decimal MarkedPrice { get; set; }
        public decimal UnitValue { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public int BranchId { get; set; }
        public decimal CostPrice { get; set; }  // 🔹 ADD THIS
        public decimal Profit { get; set; }     // 🔹 ADD THIS
        public decimal costTotal { get; set; } // 🔹 ADD THIS
        public bool IsRepack { get; set; }

        // 🔹 What shows in the list
        public decimal Total =>  Quantity * Price;
        public decimal SubTotal { get; set; }
        public string Display => $"{Name} {UnitType}";

    }
}
