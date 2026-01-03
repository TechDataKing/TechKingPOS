namespace TechKingPOS.App.Models
{
    public class SalesSummary
    {
        public int ReceiptCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal AmountPaid { get; set; }
        public int BranchId { get; set; }


        public decimal Profit => TotalSales - Tax - Discount;

    public decimal SubTotal { get; set; }

    // ================= DISCOUNT =================
    public bool HasDiscount { get; set; }

    // "Fixed", "Percentage", "Conditional"
    public string? DiscountType { get; set; }

    // 1000 OR 10 (%)
    public decimal DiscountValue { get; set; }

    // Calculated money removed (e.g. 500)
    public decimal DiscountAmount { get; set; }

    // ================= TOTAL =================
    public decimal Total { get; set; }
}

    }

