using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;

namespace TechKingPOS.App.Data
{
    public static class SalesRepository
    {
        public static string SaveSale(
            List<SaleItem> items,
            string cashier,
            decimal amountPaid)
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                LoggerService.Info("💾", "SALE", "Saving sale started");

                string receiptNumber = ReceiptNumberService.Generate();
                string createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                decimal total = items.Sum(i => i.Total);
                decimal subtotal = Math.Round(total / 1.16m, 2);
                decimal tax = total - subtotal;
                decimal discount = 0m;

                // ================= TRANSACTION HEADER =================
                var headerCmd = connection.CreateCommand();
                headerCmd.Transaction = transaction;
                headerCmd.CommandText = @"
                    INSERT INTO Transactions (
                        ReceiptNumber,
                        Cashier,
                        Total,
                        Tax,
                        Discount,
                        AmountPaid,
                        CreatedAt
                    )
                    VALUES (
                        @receipt,
                        @cashier,
                        @total,
                        @tax,
                        @discount,
                        @paid,
                        @created
                    );
                ";

                headerCmd.Parameters.AddWithValue("@receipt", receiptNumber);
                headerCmd.Parameters.AddWithValue("@cashier", cashier);
                headerCmd.Parameters.AddWithValue("@total", total);
                headerCmd.Parameters.AddWithValue("@tax", tax);
                headerCmd.Parameters.AddWithValue("@discount", discount);
                headerCmd.Parameters.AddWithValue("@paid", amountPaid);
                headerCmd.Parameters.AddWithValue("@created", createdAt);

                headerCmd.ExecuteNonQuery();

                LoggerService.Info(
                    "🧾",
                    "SALE",
                    "Transaction header saved",
                    receiptNumber
                );

                // ================= LINE ITEMS =================
                foreach (var item in items)
                {
                    var itemCmd = connection.CreateCommand();
                    itemCmd.Transaction = transaction;
                    itemCmd.CommandText = @"
                        INSERT INTO Sales (
                            ReceiptNumber,
                            ItemName,
                            Quantity,
                            Price,
                            Total,
                            CreatedAt
                        )
                        VALUES (
                            @receipt,
                            @name,
                            @qty,
                            @price,
                            @total,
                            @created
                        );
                    ";

                    itemCmd.Parameters.AddWithValue("@receipt", receiptNumber);
                    itemCmd.Parameters.AddWithValue("@name", item.Name);
                    itemCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@price", item.Price);
                    itemCmd.Parameters.AddWithValue("@total", item.Total);
                    itemCmd.Parameters.AddWithValue("@created", createdAt);

                    itemCmd.ExecuteNonQuery();
                }

                LoggerService.Info(
                    "📦",
                    "SALE",
                    "Sale items saved",
                    $"Count={items.Count}"
                );

                transaction.Commit();

                LoggerService.Info(
                    "✅",
                    "SALE",
                    "Sale committed successfully",
                    receiptNumber
                );

                return receiptNumber;
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                LoggerService.Error(
                    "❌",
                    "SALE",
                    "Sale failed — rollback executed",
                    ex
                );

                throw;
            }
        }
    }
}
