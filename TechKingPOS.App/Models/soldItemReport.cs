namespace TechKingPOS.App.Models
{
    public class SoldItemReport
    {
        public string ReceiptNumber { get; set; } = "";
        public string ItemName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }
}
