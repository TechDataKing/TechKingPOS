using System;

namespace TechKingPOS.App.Models
{
    public class Activity
    {
        public int Id { get; set; }

        public string EntityType { get; set; } = null!;
        public int? EntityId { get; set; }
        public string? EntityName { get; set; }

        public string Action { get; set; } = null!;

        // BASE UNIT ONLY
        public decimal QuantityChange { get; set; }

        // Display context
        public string? UnitType { get; set; }
        public decimal? UnitValue { get; set; }

        public decimal? Price { get; set; }
        // Snapshots
        public string? BeforeValue { get; set; }
        public string? AfterValue { get; set; }

        public string? Reason { get; set; }

        public string PerformedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int BranchId { get; set; }

    }
}
