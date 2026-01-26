using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;
using TechKingPOS.App.Security;

namespace TechKingPOS.App.Data
{
    public static class DamagedRepository
    {
        // ================= ADD DAMAGE =================
        public static void AddDamage(DamagedItem item)
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            using var tx = connection.BeginTransaction();

            // 1️⃣ Reduce stock
            var reduceCmd = connection.CreateCommand();
            reduceCmd.CommandText = @"
                UPDATE Items
                SET Quantity = Quantity - $qty
                WHERE Id = $id AND BranchId = @branchId
            ";
            reduceCmd.Parameters.AddWithValue("$qty", item.Quantity);
            reduceCmd.Parameters.AddWithValue("$id", item.ItemId);
            reduceCmd.Parameters.AddWithValue("@branchId", SessionContext.EffectiveBranchId);
            reduceCmd.ExecuteNonQuery();

            // 2️⃣ Insert damaged record
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO DamagedItems
                (
                    ItemId, ItemName, Alias, Unit,
                    Quantity, MarkedPrice, SellingPrice,
                    Reason, RecordedBy, DamagedAt, BranchId
                )
                VALUES
                (
                    $itemId, $name, $alias, $unit,
                    $qty, $mp, $sp,
                    $reason, $by, $date, $branchId
                )
            ";
            cmd.Parameters.AddWithValue("$branchId", SessionContext.EffectiveBranchId);
            cmd.Parameters.AddWithValue("$itemId", item.ItemId);
            cmd.Parameters.AddWithValue("$name", item.ItemName);
            cmd.Parameters.AddWithValue("$alias", item.Alias);
            cmd.Parameters.AddWithValue("$unitType", item.UnitType);
            cmd.Parameters.AddWithValue("$qty", item.Quantity);
            cmd.Parameters.AddWithValue("$mp", item.MarkedPrice);
            cmd.Parameters.AddWithValue("$sp", item.SellingPrice);
            cmd.Parameters.AddWithValue("$reason", item.Reason);
            cmd.Parameters.AddWithValue("$by", item.RecordedBy);
            cmd.Parameters.AddWithValue("$date", item.DamagedAt.ToString("O"));

            cmd.ExecuteNonQuery();

            tx.Commit();

            LoggerService.Info(
                "⚠️",
                "STOCK",
                "Damaged item recorded",
                $"{item.ItemName} x{item.Quantity}"
            );
        }

        // ================= LOAD ALL =================
        public static List<DamagedItem> GetAll()
        {
            var list = new List<DamagedItem>();

            using var connection = DbService.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    Id, ItemId, ItemName, Alias, UnitType,
                    Quantity, MarkedPrice, SellingPrice,
                    Reason, RecordedBy, DamagedAt
                FROM DamagedItems
                WHERE BranchId = $branchId
                ORDER BY DamagedAt DESC
            ";
            cmd.Parameters.AddWithValue("$branchId", SessionContext.EffectiveBranchId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new DamagedItem
                {
                    Id = reader.GetInt32(0),
                    ItemId = reader.GetInt32(1),
                    ItemName = reader.GetString(2),
                    Alias = reader.GetString(3),
                    UnitType = reader.GetString(4),
                    Quantity = reader.GetDecimal(5),
                    MarkedPrice = reader.GetDecimal(6),
                    SellingPrice = reader.GetDecimal(7),
                    Reason = reader.GetString(8),
                    RecordedBy = reader.GetString(9),
                    DamagedAt = DateTime.Parse(reader.GetString(10))
                });
            }

            return list;
        }

        // ================= DELETE (RESTORE STOCK) =================
        public static void Delete(DamagedItem item)
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            using var tx = connection.BeginTransaction();

            // 1️⃣ Restore stock
            var restoreCmd = connection.CreateCommand();
            restoreCmd.CommandText = @"
                UPDATE Items
                SET Quantity = Quantity + $qty
                WHERE Id = $id
            ";
            restoreCmd.Parameters.AddWithValue("$qty", item.Quantity);
            restoreCmd.Parameters.AddWithValue("$id", item.ItemId);
            restoreCmd.ExecuteNonQuery();

            // 2️⃣ Delete damaged record
            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM DamagedItems WHERE Id = $id";
            deleteCmd.Parameters.AddWithValue("$id", item.Id);
            deleteCmd.ExecuteNonQuery();

            tx.Commit();

            LoggerService.Info(
                "♻️",
                "STOCK",
                "Damaged item deleted & stock restored",
                item.ItemName
            );
        }
    }
}
