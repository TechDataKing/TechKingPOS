using System;

namespace TechKingPOS.App.Models
{
    public class Expense
    {
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public string Category { get; set; }

        public string Description { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; }

        public DateTime CreatedAt { get; set; }
        public int BranchId { get; set; }

    }
}
