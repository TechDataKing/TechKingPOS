using System;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;
using TechKingPOS.App.Security;

namespace TechKingPOS.App.Data
{
    public static class CreditRepository
    {
        public static Customer FindCustomer(string search)
        {
            using var conn = DbService.GetConnection();
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, Phone
                FROM Customers
                WHERE (branchId = 0 OR BranchId = @branchId) 
                AND (Name = @s OR Phone = @s)
                LIMIT 1;
            ";
            cmd.Parameters.AddWithValue("@branchId", SessionContext.CurrentBranchId);
            cmd.Parameters.AddWithValue("@s", search);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            return new Customer
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                Phone = r.IsDBNull(2) ? null : r.GetString(2)
            };
        }
        private static int GetLatestCreditId(int customerId)
            {
                using var conn = DbService.GetConnection();
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id
                    FROM Credits
                    WHERE CustomerId = @cid
                    ORDER BY Id DESC
                    LIMIT 1;
                ";
                cmd.Parameters.AddWithValue("@cid", customerId);
                cmd.Parameters.AddWithValue("@branchId", SessionContext.CurrentBranchId);

                var result = cmd.ExecuteScalar();
                if (result == null)
                    throw new Exception("No credit record found for customer");

                return Convert.ToInt32(result);
            }


public static void SaveCredit(
    string receiptNumber,
    string customerName,
    string phone,
    decimal total,
    decimal paid,
    decimal balance)
{
    using var conn = DbService.GetConnection();
    conn.Open();
    using var tx = conn.BeginTransaction();

    try
    {
        // 1️⃣ Ensure customer exists
        var customerCmd = conn.CreateCommand();
        customerCmd.Transaction = tx;
        customerCmd.CommandText = @"
            INSERT INTO Customers (Name, Phone, BranchId)
            VALUES (@n, @p, @branchId)
            ON CONFLICT(Name) DO NOTHING;
        ";
        customerCmd.Parameters.AddWithValue("@n", customerName);
        customerCmd.Parameters.AddWithValue("@p", phone);
        customerCmd.Parameters.AddWithValue("@branchId", SessionContext.CurrentBranchId);
        customerCmd.ExecuteNonQuery();

        // 2️⃣ Get customer id
        var idCmd = conn.CreateCommand();
        idCmd.Transaction = tx;
        idCmd.CommandText = "SELECT Id FROM Customers WHERE Name=@n LIMIT 1;";
        idCmd.Parameters.AddWithValue("@n", customerName);
        int customerId = Convert.ToInt32(idCmd.ExecuteScalar());

        // 3️⃣ Insert / update credit
        var creditCmd = conn.CreateCommand();
        creditCmd.Transaction = tx;
        creditCmd.CommandText = @"
            INSERT INTO Credits (CustomerId, ReceiptNumber, Total, Paid, Balance, BranchId, CreatedAt)
            VALUES (@cid, @r, @t, @p, @b, @branchId, @c)
            ON CONFLICT(CustomerId, BranchId) DO UPDATE SET
                Total = Total + @t,
                Paid = Paid + @p,
                Balance = Balance + @b;
        ";
        creditCmd.Parameters.AddWithValue("@branchId", SessionContext.CurrentBranchId);
        creditCmd.Parameters.AddWithValue("@cid", customerId);
        creditCmd.Parameters.AddWithValue("@r", receiptNumber);
        creditCmd.Parameters.AddWithValue("@t", total);
        creditCmd.Parameters.AddWithValue("@p", paid);
        creditCmd.Parameters.AddWithValue("@b", balance);
        creditCmd.Parameters.AddWithValue("@c", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        creditCmd.ExecuteNonQuery();

        // 4️⃣ ACTIVITY LOG
        ActivityRepository.Log(conn, tx, new Activity
        {
            EntityType = "Credit",
            EntityId = customerId,
            EntityName = customerName,
            Action = "CREDIT_CREATED",
            AfterValue = $"Receipt={receiptNumber}, Total={total:0.00}, Paid={paid:0.00}, Balance={balance:0.00}",
            PerformedBy = SessionContext.CurrentUserName,
            CreatedAt = DateTime.Now
        });

        tx.Commit();
    }
    catch
    {
        tx.Rollback();
        throw;
    }
}


        public static void AddPayment(int customerId, decimal amount, string method)
{
    using var conn = DbService.GetConnection();
    conn.Open();
    using var tx = conn.BeginTransaction();

    try
    {
        // 1️⃣ Get active credit
        var creditIdCmd = conn.CreateCommand();
        creditIdCmd.Transaction = tx;
        creditIdCmd.CommandText = @"
            SELECT Id
            FROM Credits
            WHERE CustomerId = @cid AND (branchId = 0 OR BranchId = @branchId)
            ORDER BY Id DESC
            LIMIT 1;
        ";
        creditIdCmd.Parameters.AddWithValue("@cid", customerId);
        creditIdCmd.Parameters.AddWithValue("@branchId", SessionContext.CurrentBranchId);
        var creditIdObj = creditIdCmd.ExecuteScalar();
        if (creditIdObj == null)
            throw new Exception("No credit record found.");

        int creditId = Convert.ToInt32(creditIdObj);

        // 2️⃣ Insert payment
        var paymentCmd = conn.CreateCommand();
        paymentCmd.Transaction = tx;
        paymentCmd.CommandText = @"
            INSERT INTO CreditPayments (CreditId, Amount, Method, BranchId, CreatedAt)
            VALUES (@creditId, @a, @m, @branchId, @c);
        ";
        paymentCmd.Parameters.AddWithValue("@creditId", creditId);
        paymentCmd.Parameters.AddWithValue("@a", amount);
        paymentCmd.Parameters.AddWithValue("@m", method);
        paymentCmd.Parameters.AddWithValue("@branchId", SessionContext.CurrentBranchId);
        paymentCmd.Parameters.AddWithValue("@c", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        paymentCmd.ExecuteNonQuery();

        // 3️⃣ Update credit totals
        var updateCmd = conn.CreateCommand();
        updateCmd.Transaction = tx;
        updateCmd.CommandText = @"
            UPDATE Credits
            SET Paid = Paid + @a,
                Balance = Balance - @a
            WHERE Id = @creditId AND (branchId = 0 OR BranchId = @branchId);
        ";
        updateCmd.Parameters.AddWithValue("@a", amount);
        updateCmd.Parameters.AddWithValue("@creditId", creditId);
        updateCmd.ExecuteNonQuery();

        // 4️⃣ ACTIVITY LOG
        ActivityRepository.Log(conn, tx, new Activity
        {
            EntityType = "CreditPayment",
            EntityId = creditId,
            Action = "CREDIT_PAYMENT_ADDED",
            AfterValue = $"Amount={amount:0.00}, Method={method}",
            PerformedBy = SessionContext.CurrentUserName,
            CreatedAt = DateTime.Now
        });
        ActivityRepository.Log(conn, tx, new Activity
        {
            EntityType = "Credit Payment",
            EntityId = customerId,
            Action = "CREDIT_PAYMENT_ADDED",
            AfterValue = $"PaymentAdded={amount:0.00}, Method={method}",
            PerformedBy = SessionContext.CurrentUserName,
            BranchId = SessionContext.CurrentBranchId,
            CreatedAt = DateTime.Now
        });

        tx.Commit();
    }
    catch
    {
        tx.Rollback();
        throw;
    }
}

        public static decimal GetCustomerBalance(int customerId)
{
    using var conn = DbService.GetConnection();
    conn.Open();

    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        SELECT Balance
        FROM Credits
        WHERE CustomerId = @cid AND (branchId = 0 OR BranchId = @branchId)
        ORDER BY Id DESC
        LIMIT 1;
    ";

    cmd.Parameters.AddWithValue("@cid", customerId);
    cmd.Parameters.AddWithValue("@branchId", SessionContext.CurrentBranchId);

    var result = cmd.ExecuteScalar();
    return result == null ? 0m : Convert.ToDecimal(result);
}

    }
}
