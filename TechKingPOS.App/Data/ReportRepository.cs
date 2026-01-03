using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;
using TechKingPOS.App.Security;

namespace TechKingPOS.App.Data
{
    public static class ReportsRepository
    {
        // ================= SALES SUMMARY =================
        public static SalesSummary GetSalesSummary(
            DateTime from,
            DateTime to, int branchId)
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            LoggerService.Info(
                "📊",
                "REPORT",
                "Loading sales summary",
                $"{from:yyyy-MM-dd} → {to:yyyy-MM-dd}"
            );

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    COUNT(DISTINCT ReceiptNumber),
                    IFNULL(SUM(Total), 0),
                    IFNULL(SUM(Tax), 0),
                    IFNULL(SUM(Discount), 0),
                    IFNULL(SUM(AmountPaid), 0)
                FROM Transactions
                WHERE CreatedAt BETWEEN @from AND @to
                AND (@branchId = 0 OR BranchId = @branchId);
            ";

            cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@branchId", branchId);
            using var reader = cmd.ExecuteReader();
                        reader.Read();

            var summary = new SalesSummary
            {
                ReceiptCount = reader.GetInt32(0),
                TotalSales   = reader.GetDecimal(1),
                Tax          = reader.GetDecimal(2),
                Discount     = reader.GetDecimal(3),
                AmountPaid   = reader.GetDecimal(4)
            };

            LoggerService.Info(
                "✅",
                "REPORT",
                "Sales summary loaded",
                $"Receipts={summary.ReceiptCount}, Total={summary.TotalSales}"
            );

            return summary;
        }

        // ================= SOLD ITEMS =================
        public static List<SoldItemReport> GetSoldItems(
            DateTime from,
            DateTime to, int branchId)
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            LoggerService.Info(
                "📦",
                "REPORT",
                "Loading sold items",
                $"{from:yyyy-MM-dd} → {to:yyyy-MM-dd}"
            );

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    ReceiptNumber,
                    ItemName,
                    SUM(Quantity),
                    Price,
                    SUM(Total)
                FROM Sales
                WHERE CreatedAt BETWEEN @from AND @to
                AND (@branchId = 0 OR BranchId = @branchId)
                GROUP BY ReceiptNumber, ItemName, Price
                ORDER BY CreatedAt ASC;
            ";

            cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@branchId", branchId);

            var result = new List<SoldItemReport>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new SoldItemReport
                {
                    ReceiptNumber = reader.GetString(0),
                    ItemName      = reader.GetString(1),
                    Quantity      = reader.GetInt32(2),
                    Price         = reader.GetDecimal(3),
                    Total         = reader.GetDecimal(4)
                });
            }

            LoggerService.Info(
                "✅",
                "REPORT",
                "Sold items loaded",
                $"Items={result.Count}"
            );

            return result;
        }
    }
}
