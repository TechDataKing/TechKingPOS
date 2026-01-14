using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Threading;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;
using TechKingPOS.App.Security;
namespace TechKingPOS.App.Data
{
    public static class ItemRepository
    {  private static long _version = 0;

    public static long Version => _version;

    private static void Touch()
    {
        Interlocked.Increment(ref _version);
    }

        // ================= INSERT =================
       public static void InsertItem(
            string name,
            string alias,
            decimal markedPrice,
            decimal sellingPrice,
            decimal quantity,
            string unitType,
            decimal? unitValue)
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Items
                (BranchId, Name, Alias, MarkedPrice, SellingPrice, Quantity, UnitType, UnitValue, TargetQuantity, CreatedAt)
                VALUES
                ($branchId, $name, $alias, $mp, $sp, $qty, $unitType, $unitValue, NULL, $created);
            ";

            cmd.Parameters.AddWithValue("$branchId", SessionContext.CurrentBranchId);
            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$alias", alias);
            cmd.Parameters.AddWithValue("$mp", markedPrice);
            cmd.Parameters.AddWithValue("$sp", sellingPrice);
            cmd.Parameters.AddWithValue("$qty", quantity);
            cmd.Parameters.AddWithValue("$unitType", unitType);
            cmd.Parameters.AddWithValue(
                "$unitValue",
                unitValue.HasValue ? unitValue.Value : DBNull.Value
            );
            cmd.Parameters.AddWithValue("$created", DateTime.UtcNow);

            cmd.ExecuteNonQuery();
            Touch();

            long newItemId;
            using (var idCmd = connection.CreateCommand())
            {
                idCmd.CommandText = "SELECT last_insert_rowid();";
                newItemId = (long)idCmd.ExecuteScalar();
            }

            LoggerService.Info("💾", "DB", "Item inserted", name);

            ActivityRepository.Log(new Activity
            {
                EntityType = "Item",
                EntityId = (int)newItemId,
                EntityName = name,
                Action = "ADD_ITEM",
                QuantityChange = quantity,
                UnitType = unitType,
                UnitValue = unitValue,
                BeforeValue = null,
                AfterValue = quantity.ToString(),
                Reason = "New item created",
                PerformedBy = SessionContext.CurrentUserName,
                BranchId = SessionContext.CurrentBranchId,
                CreatedAt = DateTime.UtcNow
            });
        } // ================= LOAD ALL =================
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
                    UnitType, TargetQuantity
                FROM Items
                WHERE BranchId = $branchId
                ORDER BY Name ASC;
            ";

            cmd.Parameters.AddWithValue("$branchId", SessionContext.CurrentBranchId);

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
                    UnitType, TargetQuantity
                FROM Items
                WHERE BranchId = $branchId AND (Name LIKE $q OR Alias LIKE $q)
                ORDER BY Name ASC;
            ";

            cmd.Parameters.AddWithValue("$branchId", SessionContext.CurrentBranchId);
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
                WHERE BranchId = $branchId AND TargetQuantity IS NULL
                AND Name LIKE $q
                ORDER BY Name ASC;
            ";

            cmd.Parameters.AddWithValue("$branchId", SessionContext.CurrentBranchId);
            cmd.Parameters.AddWithValue("$q", $"%{text}%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new ItemLookup
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Quantity = reader.GetDecimal(2)
                });
            }

            return items;
        }
public static ItemModel? GetByNameOrAlias(string name, string? alias)
{
    using var connection = DbService.GetConnection();
    connection.Open();

    using var cmd = connection.CreateCommand();
    cmd.CommandText = @"
        SELECT
            Id, Name, Alias,
            MarkedPrice, SellingPrice,
            Quantity, UnitType, UnitValue
        FROM Items
        WHERE
            BranchId = $branchId AND (  
            LOWER(Name) = LOWER($name)
            OR (
                $alias IS NOT NULL
                AND $alias <> ''
                AND LOWER(Alias) = LOWER($alias)
            ) 
            )
        LIMIT 1;
    ";
    cmd.Parameters.AddWithValue("$branchId", SessionContext.CurrentBranchId);
    cmd.Parameters.AddWithValue("$name", name.Trim());
    cmd.Parameters.AddWithValue(
        "$alias",
        string.IsNullOrWhiteSpace(alias) ? DBNull.Value : alias.Trim()
    );

    using var reader = cmd.ExecuteReader();

    if (!reader.Read())
        return null;

    return new ItemModel
    {
        Id = reader.GetInt32(0),
        Name = reader.GetString(1),
        Alias = reader.IsDBNull(2) ? "" : reader.GetString(2),
        MarkedPrice = reader.GetDecimal(3),
        SellingPrice = reader.GetDecimal(4),
        Quantity = reader.GetDecimal(5),
        UnitType = reader.GetString(6),
         UnitValue = reader.IsDBNull(7) ? null : reader.GetDecimal(7)
       
    };
}

// ================= ADD STOCK =================
        public static void AddStock(
            int itemId,
            decimal addQuantity,
            decimal markedPrice,
            decimal sellingPrice)
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            decimal beforeQty = 0;
            string name = "";
            string unitType = "";
            decimal? unitValue = null;

            using (var readCmd = connection.CreateCommand())
            {
                readCmd.CommandText = @"
                    SELECT Name, Quantity, UnitType, UnitValue
                    FROM Items WHERE Id = $id;
                ";
                readCmd.Parameters.AddWithValue("$id", itemId);

                using var reader = readCmd.ExecuteReader();
                if (reader.Read())
                {
                    name = reader.GetString(0);
                    beforeQty = reader.GetDecimal(1);
                    unitType = reader.GetString(2);
                    unitValue = reader.IsDBNull(3) ? null : reader.GetDecimal(3);
                }
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE Items
                SET 
                    Quantity = Quantity + $qty,
                    MarkedPrice = $marked,
                    SellingPrice = $selling
                WHERE Id = $id;
            ";

            cmd.Parameters.AddWithValue("$qty", addQuantity);
            cmd.Parameters.AddWithValue("$marked", markedPrice);
            cmd.Parameters.AddWithValue("$selling", sellingPrice);
            cmd.Parameters.AddWithValue("$id", itemId);

            cmd.ExecuteNonQuery();
            Touch();

            ActivityRepository.Log(new Activity
            {
                EntityType = "Item",
                EntityId = itemId,
                EntityName = name,
                Action = "STOCK_IN",
                QuantityChange = addQuantity,
                UnitType = unitType,
                UnitValue = unitValue,
                BeforeValue = beforeQty.ToString(),
                AfterValue = (beforeQty + addQuantity).ToString(),
                Reason = "Stock added",
                PerformedBy = SessionContext.CurrentUserName,
                BranchId = SessionContext.CurrentBranchId,
                CreatedAt = DateTime.UtcNow
            });
        }


        // ================= UPDATE ITEM =================
         public static void UpdateItem(
            int id,
            string name,
            string alias,
            decimal quantity,
            decimal markedPrice,
            decimal sellingPrice,
            decimal? targetQuantity)
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            decimal beforeQty = 0;

            using (var readCmd = connection.CreateCommand())
            {
                readCmd.CommandText = "SELECT Quantity FROM Items WHERE Id = $id;";
                readCmd.Parameters.AddWithValue("$id", id);
                using var reader = readCmd.ExecuteReader();
                if (reader.Read())
                    beforeQty = reader.GetDecimal(0);
            }

            using var cmd = connection.CreateCommand();
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
            Touch();

            ActivityRepository.Log(new Activity
            {
                EntityType = "Item",
                EntityId = id,
                EntityName = name,
                Action = "UPDATE_ITEM",
                QuantityChange = quantity - beforeQty,
                BeforeValue = beforeQty.ToString(),
                AfterValue = quantity.ToString(),
                Reason = "Item updated",
                PerformedBy = SessionContext.CurrentUserName,
                BranchId = SessionContext.CurrentBranchId,
                CreatedAt = DateTime.UtcNow
            });
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
            ActivityRepository.Log(new Activity
                {
                    EntityType = "Item",
                    EntityId = id,
                    Action = "SET_TARGET",
                    QuantityChange = 0,                     // ✔ explicit
                    AfterValue = target.ToString(),
                    Reason = "Target quantity set",
                     PerformedBy = SessionContext.CurrentUserName,
                    BranchId = SessionContext.CurrentBranchId,
                    CreatedAt = DateTime.UtcNow
                });

        }

        // ================= DELETE =================
        public static void DeleteItem(int id)
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            string name = "";
            decimal qty = 0;
            string unitType = "";
            decimal? unitValue = null;

            using (var readCmd = connection.CreateCommand())
            {
                readCmd.CommandText = @"
                    SELECT Name, Quantity, UnitType, UnitValue
                    FROM Items WHERE Id = $id;
                ";
                readCmd.Parameters.AddWithValue("$id", id);

                using var reader = readCmd.ExecuteReader();
                if (reader.Read())
                {
                    name = reader.GetString(0);
                    qty = reader.GetDecimal(1);
                    unitType = reader.GetString(2);
                    unitValue = reader.IsDBNull(3) ? null : reader.GetDecimal(3);
                }
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Items WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
            Touch();

            ActivityRepository.Log(new Activity
            {
                EntityType = "Item",
                EntityId = id,
                EntityName = name,
                Action = "DELETE_ITEM",
                QuantityChange = -qty,
                UnitType = unitType,
                UnitValue = unitValue,
                BeforeValue = qty.ToString(),
                AfterValue = "",
                Reason = "Item deleted",
                PerformedBy = SessionContext.CurrentUserName,
                BranchId = SessionContext.CurrentBranchId,
                CreatedAt = DateTime.UtcNow
            });
        }

        // ================= HELPER =================
        private static ItemLookup ReadItem(SqliteDataReader reader)
        {
            return new ItemLookup
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Alias = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Quantity = reader.GetDecimal(3),
                MarkedPrice = reader.GetDecimal(4),
                SellingPrice = reader.GetDecimal(5),
                UnitType = reader.GetString(6),
                TargetQuantity = reader.IsDBNull(7) ? null : reader.GetInt32(7)
            };
        }
        public static decimal GetBaseQuantity(
    SqliteConnection conn,
    SqliteTransaction tx,
    int itemId)
{
    var cmd = conn.CreateCommand();
    cmd.Transaction = tx;
    cmd.CommandText = @"
        SELECT Quantity
        FROM Items
        WHERE Id = @id
          AND BranchId = @bid;
    ";
    cmd.Parameters.AddWithValue("@id", itemId);
    cmd.Parameters.AddWithValue("@bid", SessionContext.CurrentBranchId);

    return Convert.ToDecimal(cmd.ExecuteScalar());
}

        public static void DeductStock(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int itemId,
            decimal deductQuantity,
            string reason,
            string performedBy)
        {
            decimal beforeQty = 0;
            string name = "";
            string unitType = "";
            decimal? unitValue = null;

            // READ current stock (LOCKED by transaction)
            using (var readCmd = connection.CreateCommand())
            {
                readCmd.Transaction = transaction;
                readCmd.CommandText = @"
                    SELECT Name, Quantity, UnitType, UnitValue
                    FROM Items
                    WHERE Id = $id;
                ";
                readCmd.Parameters.AddWithValue("$id", itemId);

                using var reader = readCmd.ExecuteReader();
                if (!reader.Read())
                    throw new Exception("Item not found during stock deduction");

                name = reader.GetString(0);
                beforeQty = reader.GetDecimal(1);
                unitType = reader.GetString(2);
                unitValue = reader.IsDBNull(3) ? null : reader.GetDecimal(3);
            }

            decimal afterQty = beforeQty - deductQuantity;

            // ❗ Enforce NO negative stock if disabled
            if (!SettingsCache.Current.AllowNegativeStock && afterQty < 0)
                throw new InvalidOperationException(
                    $"Insufficient stock for {name}. Available: {beforeQty}"
                );

            // UPDATE stock
            using (var updateCmd = connection.CreateCommand())
            {
                updateCmd.Transaction = transaction;
                updateCmd.CommandText = @"
                    UPDATE Items
                    SET Quantity = $qty
                    WHERE Id = $id;
                ";
                updateCmd.Parameters.AddWithValue("$qty", afterQty);
                updateCmd.Parameters.AddWithValue("$id", itemId);
                updateCmd.ExecuteNonQuery();
            }

            // ACTIVITY (same transaction-safe connection)
            ActivityRepository.Log(
                connection,
                transaction,
                new Activity
                {
                    EntityType = "Item",
                    EntityId = itemId,
                    EntityName = name,
                    Action = "STOCK_OUT",
                    QuantityChange = -deductQuantity,
                    UnitType = unitType,
                    UnitValue = unitValue,
                    BeforeValue = beforeQty.ToString(),
                    AfterValue = afterQty.ToString(),
                    Reason = reason,
                    PerformedBy = performedBy,
                    BranchId = SessionContext.CurrentBranchId,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
        public static decimal GetMarkedPrice(
    SqliteConnection connection,
    SqliteTransaction transaction,
    int itemId)
{
    using var cmd = connection.CreateCommand();
    cmd.Transaction = transaction;
    cmd.CommandText = @"
        SELECT MarkedPrice
        FROM Items
        WHERE Id = @id
          AND BranchId = @branchId;
    ";

    cmd.Parameters.AddWithValue("@id", itemId);
    cmd.Parameters.AddWithValue("@branchId", SessionContext.CurrentBranchId);

    object? result = cmd.ExecuteScalar();

    if (result == null || result == DBNull.Value)
        throw new InvalidOperationException("Marked price not found for item");

    return Convert.ToDecimal(result);
}


    }
}
