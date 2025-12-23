namespace TechKingPOS.App.Models
{
    public class PaymentResult
    {
        public decimal Total { get; }
        public decimal CashAmount { get; private set; }
        public decimal MpesaAmount { get; private set; }
        public decimal AmountPaid => CashAmount + MpesaAmount;
        public decimal Balance => Total - AmountPaid;
        public decimal Change => AmountPaid > Total ? AmountPaid - Total : 0;

        public bool IsCredit => Balance > 0;

        public string? CustomerName { get; private set; }
        public string? Phone { get; private set; }

        public PaymentResult(decimal total)
        {
            Total = total;
        }

        public void SetPayment(
            decimal cash,
            decimal mpesa,
            string? customerName,
            string? phone)
        {
            CashAmount = cash;
            MpesaAmount = mpesa;
            CustomerName = customerName;
            Phone = phone;
        }
    }
}
