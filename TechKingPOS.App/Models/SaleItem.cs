namespace TechKingPOS.App.Models
{
    public class SaleItem
    {
        public int ItemId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;   // 🔹 ADD THIS
        public string Unit { get; set; } = string.Empty;
        public decimal MarkedPrice { get; set; }

        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public decimal Total => Quantity * Price;

        // 🔹 What shows in the list
       
        public decimal SubTotal { get; set; }
        public string Display => $"{Name} {Unit}";

    }
}
