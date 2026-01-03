using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;

namespace TechKingPOS.App.Data
{
    public static class RepackRepository
    {
        // ================= ADD RULE =================
        public static void AddRule(
            int itemId,
            string itemName,
            decimal unitValue,
            string unitType,
            decimal sellingPrice
        )
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO RepackRules
                (ItemId, ItemName, UnitValue, UnitType, SellingPrice, CreatedAt)
                VALUES
                ($itemId, $itemName, $unitValue, $unit, $price, $created)
            ";

            cmd.Parameters.AddWithValue("$itemId", itemId);
            cmd.Parameters.AddWithValue("$itemName", itemName);
            cmd.Parameters.AddWithValue("$unitValue", unitValue);
            cmd.Parameters.AddWithValue("$unit", unitType);
            cmd.Parameters.AddWithValue("$price", sellingPrice);
            cmd.Parameters.AddWithValue("$created", DateTime.UtcNow);

            cmd.ExecuteNonQuery();

            LoggerService.Info("📦", "REPACK", "Rule added", itemName);
        }

        // ================= GET RULES FOR ITEM =================
        public static List<RepackRuleModel> GetRulesForItem(int itemId)
        {
            var list = new List<RepackRuleModel>();

            using var connection = DbService.GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    Id,
                    ItemId,
                    ItemName,
                    UnitValue,
                    UnitType,
                    SellingPrice,
                    IsActive,
                    CreatedAt
                FROM RepackRules
                WHERE ItemId = $itemId
                
                ORDER BY UnitValue ASC
            ";

            cmd.Parameters.AddWithValue("$itemId", itemId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(Read(reader));
            }

            return list;
        }

        // ================= DISABLE RULE =================
        public static void DisableRule(int ruleId)
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE RepackRules
                SET IsActive = 0
                WHERE Id = $id
            ";

            cmd.Parameters.AddWithValue("$id", ruleId);
            cmd.ExecuteNonQuery();
        }

        // ================= HELPER =================
        private static RepackRuleModel Read(SqliteDataReader r)
        {
            return new RepackRuleModel
            {
                Id = r.GetInt32(0),
                ItemId = r.GetInt32(1),
                ItemName = r.GetString(2),
                UnitValue = r.GetDecimal(3),
                UnitType = r.GetString(4),
                SellingPrice = r.GetDecimal(5),
                IsActive = r.GetInt32(6) == 1,
                CreatedAt = DateTime.Parse(r.GetString(7))
            };
        }

        public static void UpdateRule(
    int ruleId,
    decimal unitValue,
    decimal sellingPrice,
    bool isActive
)
{
    using var connection = DbService.GetConnection();
    connection.Open();

    using var cmd = connection.CreateCommand();
    cmd.CommandText = @"
        UPDATE RepackRules
        SET UnitValue = $unitValue,
            SellingPrice = $price,
            IsActive = $active
        WHERE Id = $id
    ";

    cmd.Parameters.AddWithValue("$unitValue", unitValue);
    cmd.Parameters.AddWithValue("$price", sellingPrice);
    cmd.Parameters.AddWithValue("$active", isActive ? 1 : 0);
    cmd.Parameters.AddWithValue("$id", ruleId);

    cmd.ExecuteNonQuery();
}
// ================= DELETE RULE =================
public static void DeleteRule(int ruleId)
{
    using var connection = DbService.GetConnection();
    connection.Open();

    using var cmd = connection.CreateCommand();
    cmd.CommandText = @"
        DELETE FROM RepackRules
        WHERE Id = $id
    ";

    cmd.Parameters.AddWithValue("$id", ruleId);
    cmd.ExecuteNonQuery();
}


    }
}
