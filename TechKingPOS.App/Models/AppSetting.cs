using System.Collections.Generic;

namespace TechKingPOS.App.Models
{
    public class AppSetting
    {
        public int Id { get; set; }

        // ================= BUSINESS =================
        public string BusinessName { get; set; }
        public string BranchName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string PhysicalAddress { get; set; }
        public string ReceiptFooter { get; set; }

        // ================= RECEIPT & PRINT =================
        public bool AutoPrintReceipt { get; set; }
        public bool ShowCashierOnReceipt { get; set; }
        public bool ShowLogoOnReceipt { get; set; }

        // "58mm", "80mm", etc
        public string PaperSize { get; set; }

        // Copies to print
        public int ReceiptCopies { get; set; }

        // ================= SALES & PAYMENT =================
        public bool AllowNegativeStock { get; set; }
        public bool AllowPriceEditDuringSale { get; set; }
        public bool EnableCreditSales { get; set; }

        public bool VatEnabled { get; set; }
        public decimal VatPercent { get; set; }

        // ================= USERS & SECURITY =================
        public bool RequireLogin { get; set; }
        public bool AutoLogout { get; set; }
        public bool AllowVoidSales { get; set; }
        public bool AllowWorkersEditPrices { get; set; }
        public bool AllowWorkersGiveDiscounts { get; set; }

        // ================= LICENSE =================
        public string LicenseKey { get; set; }
        public string LicenseExpiry { get; set; }

        // ================= DISCOUNTS (MAIN SWITCH) =================
        public bool EnableDiscounts { get; set; }

        // Allowed types
        public bool AllowFixedDiscount { get; set; }
        public bool AllowPercentageDiscount { get; set; }
        public bool AllowConditionalDiscount { get; set; }

        // Limits
        public decimal MaxFixedDiscount { get; set; }
        public decimal MaxPercentageDiscount { get; set; }

        // ================= CONDITIONAL DISCOUNT =================
        public bool CondValueFixed { get; set; }
        public bool CondValuePercent { get; set; }

        // Always ranges in your UI
        public bool CondBasedRanges { get; set; }

        // Optional “above X → discount Y”
        public decimal ConditionalMinSubtotal { get; set; }
        public decimal ConditionalDiscountAmount { get; set; }

        // ================= DISCOUNT RANGES =================
        public List<DiscountRange> DiscountRanges { get; set; } = new();
        public int BranchId { get; set; }

    }
}
