using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;
using System.Windows;
using TechKingPOS.App.Services;
using TechKingPOS.App.Security;

namespace TechKingPOS.App.Data
{
    public static class SettingsRepository
    {
        public static AppSetting Get()
        {
            using var conn = new SqliteConnection(DbService.ConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM AppSettings LIMIT 1";

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            var s = new AppSetting
            {
                Id = reader.GetInt32(0),

                BusinessName = reader.GetString(1),
                BranchName = reader.GetString(2),
                Phone = reader.GetString(3),
                Email = reader.GetString(4),
                PhysicalAddress = reader.GetString(5),
                ReceiptFooter = reader.GetString(6),

                AutoPrintReceipt = reader.GetBoolean(7),
                ShowCashierOnReceipt = reader.GetBoolean(8),
                ShowLogoOnReceipt = reader.GetBoolean(9),
                PaperSize = reader.GetString(10),
                ReceiptCopies = reader.GetInt32(11),

                AllowNegativeStock = reader.GetBoolean(12),
                AllowPriceEditDuringSale = reader.GetBoolean(13),
                EnableCreditSales = reader.GetBoolean(14),

                VatEnabled = reader.GetBoolean(15),
                VatPercent = reader.GetDecimal(16),

                RequireLogin = reader.GetBoolean(17),
                AutoLogout = reader.GetBoolean(18),
                AllowVoidSales = reader.GetBoolean(19),
                AllowWorkersEditPrices = reader.GetBoolean(20),
                AllowWorkersGiveDiscounts = reader.GetBoolean(21),

                LicenseKey = reader.GetString(22),
                LicenseExpiry = reader.GetString(23),

                EnableDiscounts = reader.GetBoolean(24),

                AllowFixedDiscount = reader.GetBoolean(25),
                AllowPercentageDiscount = reader.GetBoolean(26),
                AllowConditionalDiscount = reader.GetBoolean(27),

                MaxFixedDiscount = reader.GetDecimal(28),
                MaxPercentageDiscount = reader.GetDecimal(29),

                CondValueFixed = reader.GetBoolean(30),
                CondValuePercent = reader.GetBoolean(31),
                CondBasedRanges = reader.GetBoolean(32),

                ConditionalMinSubtotal = reader.GetDecimal(33),
                ConditionalDiscountAmount = reader.GetDecimal(34)
            };

            s.DiscountRanges = DiscountRangeRepository.GetAll();

            return s;
        }

        public static void Save(AppSetting s)
        {
            var oldSettings = Get();   // snapshot BEFORE save

            LogChangedSettings(oldSettings, s);

            using var conn = new SqliteConnection(DbService.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT OR REPLACE INTO AppSettings VALUES
(
    1,
    $name,$branch,$phone,$email,$addr,$footer,
    $autoPrint,$showCashier,$showLogo,$paper,$copies,
    $negStock,$editPrice,$credit,
    $vat,$vatp,
    $login,$logout,$void,$workerEdit,$workerDisc,
    $key,$expiry,
    $enableDisc,$fixed,$percent,$conditional,
    $maxFixed,$maxPercent,
    $cvf,$cvp,$ranges,
    $minSubtotal,$condAmount
);
";

            cmd.Parameters.AddWithValue("$name", s.BusinessName ?? "");
            cmd.Parameters.AddWithValue("$branch", s.BranchName ?? "");
            cmd.Parameters.AddWithValue("$phone", s.Phone ?? "");
            cmd.Parameters.AddWithValue("$email", s.Email ?? "");
            cmd.Parameters.AddWithValue("$addr", s.PhysicalAddress ?? "");
            cmd.Parameters.AddWithValue("$footer", s.ReceiptFooter ?? "");

            cmd.Parameters.AddWithValue("$autoPrint", s.AutoPrintReceipt ? 1 : 0);
            cmd.Parameters.AddWithValue("$showCashier", s.ShowCashierOnReceipt ? 1 : 0);
            cmd.Parameters.AddWithValue("$showLogo", s.ShowLogoOnReceipt ? 1 : 0);
            cmd.Parameters.AddWithValue("$paper", s.PaperSize ?? "80mm");
            cmd.Parameters.AddWithValue("$copies", s.ReceiptCopies);

            cmd.Parameters.AddWithValue("$negStock", s.AllowNegativeStock ? 1 : 0);
            cmd.Parameters.AddWithValue("$editPrice", s.AllowPriceEditDuringSale ? 1 : 0);
            cmd.Parameters.AddWithValue("$credit", s.EnableCreditSales ? 1 : 0);

            cmd.Parameters.AddWithValue("$vat", s.VatEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("$vatp", s.VatPercent);

            cmd.Parameters.AddWithValue("$login", s.RequireLogin ? 1 : 0);
            cmd.Parameters.AddWithValue("$logout", s.AutoLogout ? 1 : 0);
            cmd.Parameters.AddWithValue("$void", s.AllowVoidSales ? 1 : 0);
            cmd.Parameters.AddWithValue("$workerEdit", s.AllowWorkersEditPrices ? 1 : 0);
            cmd.Parameters.AddWithValue("$workerDisc", s.AllowWorkersGiveDiscounts ? 1 : 0);

            cmd.Parameters.AddWithValue("$key", s.LicenseKey ?? "");
            cmd.Parameters.AddWithValue("$expiry", s.LicenseExpiry ?? "");

            cmd.Parameters.AddWithValue("$enableDisc", s.EnableDiscounts ? 1 : 0);
            cmd.Parameters.AddWithValue("$fixed", s.AllowFixedDiscount ? 1 : 0);
            cmd.Parameters.AddWithValue("$percent", s.AllowPercentageDiscount ? 1 : 0);
            cmd.Parameters.AddWithValue("$conditional", s.AllowConditionalDiscount ? 1 : 0);

            cmd.Parameters.AddWithValue("$maxFixed", s.MaxFixedDiscount);
            cmd.Parameters.AddWithValue("$maxPercent", s.MaxPercentageDiscount);

            cmd.Parameters.AddWithValue("$cvf", s.CondValueFixed ? 1 : 0);
            cmd.Parameters.AddWithValue("$cvp", s.CondValuePercent ? 1 : 0);
            cmd.Parameters.AddWithValue("$ranges", s.CondBasedRanges ? 1 : 0);

            cmd.Parameters.AddWithValue("$minSubtotal", s.ConditionalMinSubtotal);
            cmd.Parameters.AddWithValue("$condAmount", s.ConditionalDiscountAmount);

            cmd.ExecuteNonQuery();

            DiscountRangeRepository.SaveAll(s.DiscountRanges);
            
        }

        public static void UpdateField(string fieldName, object value)
        {
            using var conn = new SqliteConnection(DbService.ConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = $"UPDATE AppSettings SET {fieldName} = $val WHERE Id = 1";
            cmd.Parameters.AddWithValue("$val", value ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }
        private static void LogChangedSettings(AppSetting oldS, AppSetting newS)
{
    if (oldS == null || newS == null)
        return;

    var props = typeof(AppSetting).GetProperties();

    foreach (var prop in props)
    {
        // Skip collections / complex types
        if (prop.PropertyType == typeof(List<DiscountRange>))
            continue;

        var oldVal = prop.GetValue(oldS);
        var newVal = prop.GetValue(newS);

        string oldText = oldVal?.ToString() ?? "";
        string newText = newVal?.ToString() ?? "";

        if (oldText == newText)
            continue;

        ActivityRepository.Log(new Activity
        {
            EntityType = "Settings",
            EntityId = 1,
            EntityName = prop.Name,
            Action = "UPDATE",
            BeforeValue = oldText,
            AfterValue = newText,
            PerformedBy = SessionContext.CurrentUserName,
            BranchId = SessionContext.EffectiveBranchId,
            CreatedAt = DateTime.Now
        });
    }
}

    }
}
