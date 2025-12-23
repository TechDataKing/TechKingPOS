namespace TechKingPOS.App.Models
{
    public class ItemModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }

        public decimal MarkedPrice { get; set; }
        public decimal SellingPrice { get; set; }

        public int Quantity { get; set; }
        public string Unit { get; set; }
    }
}
