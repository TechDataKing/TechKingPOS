namespace TechKingPOS.App.Models
{
    public class AppSetting
    {
        public int Id { get; set; }

        // BUSINESS
        public string BusinessName { get; set; }
        public string Phone { get; set; }
        public string ReceiptFooter { get; set; }

        // SALES
        public bool VatEnabled { get; set; }
        public decimal VatPercent { get; set; }

        // SECURITY
        public bool RequireLogin { get; set; }

        // LICENSE
        public string LicenseKey { get; set; }
        public string LicenseExpiry { get; set; }


           // ================= DISCOUNTS =================
            public bool EnableDiscounts { get; set; }

            // Allowed discount types
            public bool AllowFixedDiscount { get; set; }
            public bool AllowPercentageDiscount { get; set; }
            public bool AllowConditionalDiscount { get; set; }

            // Limits
            public decimal MaxFixedDiscount { get; set; }        // e.g 1000
            public decimal MaxPercentageDiscount { get; set; }   // e.g 20 (%)

            // Conditional rule (PHASE 1: Above X → Give Y)
            public decimal ConditionalMinSubtotal { get; set; }  // e.g 5000
            public decimal ConditionalDiscountAmount { get; set; } // e.g 500

    }
}
