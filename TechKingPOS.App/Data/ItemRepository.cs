using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;

namespace TechKingPOS.App.Data
{
    public static class ItemRepository
    {
        // ================= INSERT =================
        public static void InsertItem(
            string name,
            string alias,
            decimal markedPrice,
            decimal sellingPrice,
            int quantity,
            string unit)
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Items
                (Name, Alias, MarkedPrice, SellingPrice, Quantity, Unit, TargetQuantity, CreatedAt)
                VALUES
                ($name, $alias, $mp, $sp, $qty, $unit, NULL, $created);
            ";

            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$alias", alias);
            cmd.Parameters.AddWithValue("$mp", markedPrice);
            cmd.Parameters.AddWithValue("$sp", sellingPrice);
            cmd.Parameters.AddWithValue("$qty", quantity);
            cmd.Parameters.AddWithValue("$unit", unit);
            cmd.Parameters.AddWithValue("$created", DateTime.UtcNow);

            cmd.ExecuteNonQuery();

            LoggerService.Info("💾", "DB", "Item inserted", name);
        }

        // ================= LOAD ALL =================
        public static List<ItemLookup> GetAllItems()
        {
            var items = new List<ItemLookup>();

            using var connection = DbService.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    Id, Name, Alias, Quantity,
                    MarkedPrice, SellingPrice,
                    Unit, TargetQuantity
                FROM Items
                ORDER BY Name ASC;
            ";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(ReadItem(reader));
            }

            return items;
        }

        // ================= SEARCH =================
        public static List<ItemLookup> SearchItems(string text)
        {
            var items = new List<ItemLookup>();

            using var connection = DbService.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    Id, Name, Alias, Quantity,
                    MarkedPrice, SellingPrice,
                    Unit, TargetQuantity
                FROM Items
                WHERE Name LIKE $q OR Alias LIKE $q
                ORDER BY Name ASC;
            ";

            cmd.Parameters.AddWithValue("$q", $"%{text}%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(ReadItem(reader));
            }

            return items;
        }

        // ================= TARGET TAB =================
        public static List<ItemLookup> GetItemsWithoutTarget(string text)
        {
            var items = new List<ItemLookup>();

            using var connection = DbService.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    Id, Name, Quantity
                FROM Items
                WHERE TargetQuantity IS NULL
                AND Name LIKE $q
                ORDER BY Name ASC;
            ";

            cmd.Parameters.AddWithValue("$q", $"%{text}%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new ItemLookup
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Quantity = reader.GetInt32(2)
                });
            }

            return items;
        }

        // ================= UPDATE ITEM =================
        public static void UpdateItem(
            int id,
            string name,
            string alias,
            int quantity,
            decimal markedPrice,
            decimal sellingPrice,
            int? targetQuantity)
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE Items SET
                    Name = $name,
                    Alias = $alias,
                    Quantity = $qty,
                    MarkedPrice = $mp,
                    SellingPrice = $sp,
                    TargetQuantity = $target
                WHERE Id = $id;
            ";

            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$alias", alias);
            cmd.Parameters.AddWithValue("$qty", quantity);
            cmd.Parameters.AddWithValue("$mp", markedPrice);
            cmd.Parameters.AddWithValue("$sp", sellingPrice);
            cmd.Parameters.AddWithValue(
                "$target",
                targetQuantity.HasValue ? targetQuantity.Value : DBNull.Value
            );
            cmd.Parameters.AddWithValue("$id", id);

            cmd.ExecuteNonQuery();

            LoggerService.Info("💾", "STOCK", "Item updated", name);
        }

        // ================= SET TARGET =================
        public static void SetTarget(int id, int target)
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE Items SET TargetQuantity = $target WHERE Id = $id;
            ";

            cmd.Parameters.AddWithValue("$target", target);
            cmd.Parameters.AddWithValue("$id", id);

            cmd.ExecuteNonQuery();
        }

        // ================= DELETE =================
        public static void DeleteItem(int id)
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Items WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);

            cmd.ExecuteNonQuery();
        }

        // ================= HELPER =================
        private static ItemLookup ReadItem(SqliteDataReader reader)
        {
            return new ItemLookup
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Alias = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Quantity = reader.GetInt32(3),
                MarkedPrice = reader.GetDecimal(4),
                SellingPrice = reader.GetDecimal(5),
                Unit = reader.GetString(6),
                TargetQuantity = reader.IsDBNull(7) ? null : reader.GetInt32(7)
            };
        }
    }
}
