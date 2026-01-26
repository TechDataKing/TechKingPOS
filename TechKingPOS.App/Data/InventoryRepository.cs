using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;
using TechKingPOS.App.Security;
using TechKingPOS.App.Services;

namespace TechKingPOS.App.Data
{
    public static class InventoryRepository
    {
        // ================= OUT OF STOCK =================
        // quantity = 0
        public static List<ItemLookup> GetOutOfStock()
        {
            return LoadWhere("Quantity = 0");
        }

        // ================= RUNNING LOW =================
        // quantity <= round(Target / 3)
        public static List<ItemLookup> GetRunningLow()
        {
            return LoadWhere(
                "TargetQuantity IS NOT NULL AND Quantity <= ROUND(TargetQuantity / 3.0)"
            );
        }

        // ================= GOOD STOCK =================
        // quantity >= round(Target / 2)
        public static List<ItemLookup> GetGoodStock()
        {
            return LoadWhere(
                "TargetQuantity IS NOT NULL AND Quantity >= ROUND(TargetQuantity / 2.0)"
            );
        }

        // ================= SHARED LOADER =================
        private static List<ItemLookup> LoadWhere(string condition)
        {
            var items = new List<ItemLookup>();

            using var connection = DbService.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = $@"
                SELECT
                    Id,
                    Name,
                    Quantity,
                    TargetQuantity
                FROM Items
                WHERE {condition} AND (@branchId = 0 OR BranchId = @branchId)
                ORDER BY Name ASC;
            ";
            cmd.Parameters.AddWithValue("@branchId", SessionContext.EffectiveBranchId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int quantity = reader.GetInt32(2);
                int? target = reader.IsDBNull(3) ? null : reader.GetInt32(3);

                items.Add(new ItemLookup
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Quantity = quantity,
                    TargetQuantity = target
                });
            }

            LoggerService.Info(
                "📦",
                "INVENTORY",
                "Inventory loaded",
                $"Condition={condition}, Count={items.Count}"
            );

            return items;
        }
    }
}
