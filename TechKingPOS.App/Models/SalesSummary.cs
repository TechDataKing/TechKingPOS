namespace TechKingPOS.App.Models
{
    public class SalesSummary
    {
        public int ReceiptCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal AmountPaid { get; set; }

        public decimal Profit => TotalSales - Tax - Discount;
    }
}
