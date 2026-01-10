namespace TechKingPOS.App.Security
{
    public static class PermissionSeed
    {
        public static readonly string[] All =
        {
            "open_sales",
            "open_add_item",
            "open_manage_stock",
            "open_credit_management",
            "open_reports",
            "open_workers",
            "open_settings",

            "stock.targets",
            "stock.edit_item",
            "stock.damaged",
            "stock.repack",

            "credit.add",
            "credit.summary",
            "credit.add_payments",
            "credit.payment_history",

            "reports.sales",
            "reports.inventory",
            "reports.credit",
            "reports.expenses",
            "reports.change_branch"
        };
    }
}
