using System;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Services;

namespace TechKingPOS.App.Data
{
    public static class PaymentRepository
    {
        public static void SavePayment(
            string receiptNumber,
            string method,
            decimal amount)
        {
            if (amount <= 0) return;

            using var connection = DbService.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Payments (
                    ReceiptNumber,
                    Method,
                    Amount,
                    CreatedAt
                )
                VALUES (
                    @receipt,
                    @method,
                    @amount,
                    @created
                );
            ";

            cmd.Parameters.AddWithValue("@receipt", receiptNumber);
            cmd.Parameters.AddWithValue("@method", method);
            cmd.Parameters.AddWithValue("@amount", amount);
            cmd.Parameters.AddWithValue(
                "@created",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            );

            cmd.ExecuteNonQuery();
        }
    }
}
