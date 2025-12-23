using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;

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

            return new AppSetting
            {
                Id = reader.GetInt32(0),
                BusinessName = reader.GetString(1),
                Phone = reader.GetString(2),
                ReceiptFooter = reader.GetString(3),
                VatEnabled = reader.GetBoolean(4),
                VatPercent = reader.GetDecimal(5),
                RequireLogin = reader.GetBoolean(6),
                LicenseKey = reader.GetString(7),
                LicenseExpiry = reader.GetString(8)
            };
        }

        public static void Save(AppSetting s)
        {
            using var conn = new SqliteConnection(DbService.ConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO AppSettings
                (Id, BusinessName, Phone, ReceiptFooter, VatEnabled, VatPercent, RequireLogin, LicenseKey, LicenseExpiry)
                VALUES
                (1, $name, $phone, $footer, $vat, $vatp, $login, $key, $expiry);
            ";

            cmd.Parameters.AddWithValue("$name", s.BusinessName ?? "");
            cmd.Parameters.AddWithValue("$phone", s.Phone ?? "");
            cmd.Parameters.AddWithValue("$footer", s.ReceiptFooter ?? "");
            cmd.Parameters.AddWithValue("$vat", s.VatEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("$vatp", s.VatPercent);
            cmd.Parameters.AddWithValue("$login", s.RequireLogin ? 1 : 0);
            cmd.Parameters.AddWithValue("$key", s.LicenseKey ?? "");
            cmd.Parameters.AddWithValue("$expiry", s.LicenseExpiry ?? "");

            cmd.ExecuteNonQuery();
        }
    }
}
