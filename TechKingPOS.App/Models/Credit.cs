namespace TechKingPOS.App.Models
{
    public class Credit
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Balance { get; set; }
        public int BranchId { get; set; }

    }
}
