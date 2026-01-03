namespace TechKingPOS.App.Models
{
    public class CreditView
    {
        public int CreditId { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Balance { get; set; }
        public int BranchId { get; set; }

        public bool IsOverdue =>
    (DateTime.Now - LastPaymentDate).TotalDays > 30;

        public DateTime LastPaymentDate { get; set; }
        public int DaysSinceLastPayment =>
            (DateTime.Now - LastPaymentDate).Days;

        public string Status =>
            DaysSinceLastPayment <= 30 ? "Performing" :
            DaysSinceLastPayment <= 180 ? "Watch" :
            DaysSinceLastPayment <= 365 ? "Doubtful" :
            "Loss";
    }
}
