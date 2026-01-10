namespace TechKingPOS.App.Security
{
    public static class PermissionMap
    {
        // WINDOW ACCESS
        public const string OpenSales = "open_sales";
        public const string OpenAddItem = "open_add_item";
        public const string OpenManageStock = "open_manage_stock";
        public const string OpenCreditManagement = "open_credit_management";
        public const string OpenReports = "open_reports";
        public const string OpenWorkers = "open_workers";
        public const string OpenSettings = "open_settings";

        // STOCK
        public const string StockTargets = "stock.targets";
        public const string StockEdit = "stock.edit_item";
        public const string StockDamaged = "stock.damaged";
        public const string StockRepack = "stock.repack";

        // CREDIT
        public const string CreditAdd = "credit.add";
        public const string CreditSummary = "credit.summary";
        public const string CreditAddPayments = "credit.add_payments";
        public const string CreditPaymentHistory = "credit.payment_history";

        // REPORTS
        public const string ReportSales = "reports.sales";
        public const string ReportInventory = "reports.inventory";
        public const string ReportCredit = "reports.credit";
        public const string ReportExpenses = "reports.expenses";
        public const string ChangeBranch = "reports.change_branch";
    }
}
