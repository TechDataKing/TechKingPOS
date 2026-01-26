using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;
using TechKingPOS.App.Security;

namespace TechKingPOS.App.Data
{
    public static class ExpenseRepository
    {
        // ================= ADD EXPENSE =================
public static void AddExpense(
    DateTime date,
    string category,
    string description,
    decimal amount,
    string paymentMethod)
{
    using var conn = DbService.GetConnection();
    conn.Open();

    using var tx = conn.BeginTransaction();

    var cmd = conn.CreateCommand();
    cmd.Transaction = tx;
    cmd.CommandText = @"
        INSERT INTO Expenses
        (Date, Category, Description, Amount, PaymentMethod, BranchId, CreatedAt)
        VALUES
        (@d, @c, @desc, @a, @m, @branchId, @created);
    ";

    cmd.Parameters.AddWithValue("@d", date.ToString("yyyy-MM-dd"));
    cmd.Parameters.AddWithValue("@c", category);
    cmd.Parameters.AddWithValue("@desc", description ?? "");
    cmd.Parameters.AddWithValue("@a", amount);
    cmd.Parameters.AddWithValue("@m", paymentMethod);
    cmd.Parameters.AddWithValue("@branchId", SessionContext.EffectiveBranchId);
    cmd.Parameters.AddWithValue("@created",
        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

    cmd.ExecuteNonQuery();

    // 👉 Get new expense id
    var idCmd = conn.CreateCommand();
    idCmd.Transaction = tx;
    idCmd.CommandText = "SELECT last_insert_rowid();";
    int expenseId = Convert.ToInt32((long)idCmd.ExecuteScalar());

    // 👉 ACTIVITY LOG
    ActivityRepository.Log(conn, tx, new Activity
    {
        EntityType = "Expense",
        EntityId = expenseId,
        EntityName = category,
        Action = "EXPENSE_CREATED",
        QuantityChange = amount,
        Reason = description,
        PerformedBy = SessionContext.CurrentUserName,
        BranchId = SessionContext.EffectiveBranchId,
        CreatedAt = DateTime.UtcNow
    });

    tx.Commit();
}


        // ================= GET EXPENSES =================
        public static List<Expense> GetExpenses(DateTime from, DateTime to)
        {
            var list = new List<Expense>();

            using var conn = DbService.GetConnection();
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Date, Category, Description, Amount, PaymentMethod, CreatedAt
                FROM Expenses
                WHERE Date BETWEEN @from AND @to AND (@branchId = 0 OR BranchId = @branchId)
                ORDER BY Date DESC;
            ";
            cmd.Parameters.AddWithValue("@branchId", SessionContext.EffectiveBranchId);
            cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Expense
                {
                    Id = r.GetInt32(0),
                    Date = DateTime.Parse(r.GetString(1)),
                    Category = r.GetString(2),
                    Description = r.IsDBNull(3) ? "" : r.GetString(3),
                    Amount = r.GetDecimal(4),
                    PaymentMethod = r.GetString(5),
                    CreatedAt = DateTime.Parse(r.GetString(6))
                });
            }

            return list;
        }

        // ================= DELETE EXPENSE =================
public static void DeleteExpense(int id)
{
    using var conn = DbService.GetConnection();
    conn.Open();

    using var tx = conn.BeginTransaction();

    // 👉 Read expense info first
    var readCmd = conn.CreateCommand();
    readCmd.Transaction = tx;
    readCmd.CommandText = @"
        SELECT Category, Amount
        FROM Expenses
        WHERE Id = @id AND BranchId = @branchId;
    ";
    readCmd.Parameters.AddWithValue("@id", id);
    readCmd.Parameters.AddWithValue("@branchId", SessionContext.EffectiveBranchId);

    string category = "Expense";
    decimal amount = 0;

    using (var r = readCmd.ExecuteReader())
    {
        if (r.Read())
        {
            category = r.GetString(0);
            amount = r.GetDecimal(1);
        }
    }

    // 👉 Delete
    var delCmd = conn.CreateCommand();
    delCmd.Transaction = tx;
    delCmd.CommandText = "DELETE FROM Expenses WHERE Id = @id AND BranchId = @branchId;";
    delCmd.Parameters.AddWithValue("@id", id);
    delCmd.Parameters.AddWithValue("@branchId", SessionContext.EffectiveBranchId);
    delCmd.ExecuteNonQuery();

    // 👉 ACTIVITY LOG
    ActivityRepository.Log(conn, tx, new Activity
    {
        EntityType = "Expense",
        EntityId = id,
        EntityName = category,
        Action = "EXPENSE_DELETED",
        QuantityChange = -amount,
        Reason = "Expense deleted",
        PerformedBy = SessionContext.CurrentUserName,
        BranchId = SessionContext.EffectiveBranchId,
        CreatedAt = DateTime.UtcNow
    });

    tx.Commit();
}

        // ================= UPDATE EXPENSE =================
public static void UpdateExpense(
    int id,
    DateTime date,
    string category,
    string description,
    decimal amount,
    string paymentMethod)
{
    using var conn = DbService.GetConnection();
    conn.Open();

    using var tx = conn.BeginTransaction();

    var cmd = conn.CreateCommand();
    cmd.Transaction = tx;
    cmd.CommandText = @"
        UPDATE Expenses
        SET
            Date = @d,
            Category = @c,
            Description = @desc,
            Amount = @a,
            PaymentMethod = @m
        WHERE Id = @id AND BranchId = @branchId;
    ";

    cmd.Parameters.AddWithValue("@branchId", SessionContext.EffectiveBranchId);
    cmd.Parameters.AddWithValue("@id", id);
    cmd.Parameters.AddWithValue("@d", date.ToString("yyyy-MM-dd"));
    cmd.Parameters.AddWithValue("@c", category);
    cmd.Parameters.AddWithValue("@desc", description ?? "");
    cmd.Parameters.AddWithValue("@a", amount);
    cmd.Parameters.AddWithValue("@m", paymentMethod);

    cmd.ExecuteNonQuery();

    // 👉 ACTIVITY LOG
    ActivityRepository.Log(conn, tx, new Activity
    {
        EntityType = "Expense",
        EntityId = id,
        EntityName = category,
        Action = "EXPENSE_UPDATED",
        QuantityChange = amount,
        Reason = description,
        PerformedBy = SessionContext.CurrentUserName,
        BranchId = SessionContext.EffectiveBranchId,
        CreatedAt = DateTime.UtcNow
    });

    tx.Commit();
}

        // ================= TOTAL EXPENSE =================
        public static decimal GetTotalExpenses(DateTime from, DateTime to)
        {
            using var conn = DbService.GetConnection();
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT IFNULL(SUM(Amount), 0)
                FROM Expenses
                WHERE Date BETWEEN @from AND @to
                AND (@branchId = 0 OR BranchId = @branchId);
            ";

            cmd.Parameters.AddWithValue("@branchId", SessionContext.EffectiveBranchId);
            cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));

            return Convert.ToDecimal(cmd.ExecuteScalar());
        }
    }
}
