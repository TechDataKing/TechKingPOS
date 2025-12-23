using System;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;

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
                WHERE Name = @s OR Phone = @s
                LIMIT 1;
            ";
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

            // Ensure customer exists
            var customerCmd = conn.CreateCommand();
            customerCmd.CommandText = @"
                INSERT INTO Customers (Name, Phone)
                VALUES (@n, @p)
                ON CONFLICT(Name) DO NOTHING;
            ";
            customerCmd.Parameters.AddWithValue("@n", customerName);
            customerCmd.Parameters.AddWithValue("@p", phone);
            customerCmd.ExecuteNonQuery();

            // Get customer id
            var idCmd = conn.CreateCommand();
            idCmd.CommandText = "SELECT Id FROM Customers WHERE Name=@n;";
            idCmd.Parameters.AddWithValue("@n", customerName);
            int customerId = Convert.ToInt32(idCmd.ExecuteScalar());

            // Insert or update credit
            var creditCmd = conn.CreateCommand();
            creditCmd.CommandText = @"
                INSERT INTO Credits (CustomerId, ReceiptNumber, Total, Paid, Balance, CreatedAt)
                VALUES (@cid, @r, @t, @p, @b, @c)
                ON CONFLICT(CustomerId) DO UPDATE SET
                    Total = Total + @t,
                    Paid = Paid + @p,
                    Balance = Balance + @b;
            ";
            creditCmd.Parameters.AddWithValue("@cid", customerId);
            creditCmd.Parameters.AddWithValue("@r", receiptNumber);
            creditCmd.Parameters.AddWithValue("@t", total);
            creditCmd.Parameters.AddWithValue("@p", paid);
            creditCmd.Parameters.AddWithValue("@b", balance);
            creditCmd.Parameters.AddWithValue("@c", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            creditCmd.ExecuteNonQuery();

            tx.Commit();
        }

        public static void AddPayment(int customerId, decimal amount, string method)
        {
            using var conn = DbService.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            // 1️⃣ Get the active credit record for this customer
            var creditIdCmd = conn.CreateCommand();
            creditIdCmd.CommandText = @"
                SELECT Id
                FROM Credits
                WHERE CustomerId = @cid
                ORDER BY Id DESC
                LIMIT 1;
            ";
            creditIdCmd.Parameters.AddWithValue("@cid", customerId);

            var creditIdObj = creditIdCmd.ExecuteScalar();
            if (creditIdObj == null)
                throw new Exception("No credit record found for customer.");

            int creditId = Convert.ToInt32(creditIdObj);

            // 2️⃣ Insert credit payment (CORRECT COLUMN)
            var paymentCmd = conn.CreateCommand();
            paymentCmd.CommandText = @"
                INSERT INTO CreditPayments (CreditId, Amount, Method, CreatedAt)
                VALUES (@creditId, @a, @m, @c);
            ";
            paymentCmd.Parameters.AddWithValue("@creditId", creditId);
            paymentCmd.Parameters.AddWithValue("@a", amount);
            paymentCmd.Parameters.AddWithValue("@m", method);
            paymentCmd.Parameters.AddWithValue("@c", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            paymentCmd.ExecuteNonQuery();

            // 3️⃣ Update credit totals
            var updateCmd = conn.CreateCommand();
            updateCmd.CommandText = @"
                UPDATE Credits
                SET Paid = Paid + @a,
                    Balance = Balance - @a
                WHERE Id = @creditId;
            ";
            updateCmd.Parameters.AddWithValue("@a", amount);
            updateCmd.Parameters.AddWithValue("@creditId", creditId);
            updateCmd.ExecuteNonQuery();

            tx.Commit();
        }

        public static decimal GetCustomerBalance(int customerId)
{
    using var conn = DbService.GetConnection();
    conn.Open();

    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        SELECT Balance
        FROM Credits
        WHERE CustomerId = @cid
        ORDER BY Id DESC
        LIMIT 1;
    ";

    cmd.Parameters.AddWithValue("@cid", customerId);

    var result = cmd.ExecuteScalar();
    return result == null ? 0m : Convert.ToDecimal(result);
}

    }
}
