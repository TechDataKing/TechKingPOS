using System;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Services;
using TechKingPOS.App.Models;
using TechKingPOS.App.Security;



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
                    BranchId,
                    CreatedAt
                )
                VALUES (
                    @receipt,
                    @method,
                    @amount,
                    @branchId,
                    @created
                );
            ";

            cmd.Parameters.AddWithValue("@receipt", receiptNumber);
            cmd.Parameters.AddWithValue("@method", method);
            cmd.Parameters.AddWithValue("@amount", amount);
            cmd.Parameters.AddWithValue("@branchId", SessionContext.EffectiveBranchId);
            cmd.Parameters.AddWithValue(
                "@created",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            );

            cmd.ExecuteNonQuery();

            var idCmd = connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            long paymentId = (long)idCmd.ExecuteScalar();

            ActivityRepository.Log(new Activity
            {
                EntityType = "Payment",
                EntityId = (int)paymentId,
                EntityName = receiptNumber,
                Action = "PAYMENT_RECEIVED",
                UnitValue = amount,
                Reason = method,
                PerformedBy = SessionContext.CurrentUserName,
                BranchId = SessionContext.EffectiveBranchId,
                CreatedAt = DateTime.Now
            });

        }
    }
}
