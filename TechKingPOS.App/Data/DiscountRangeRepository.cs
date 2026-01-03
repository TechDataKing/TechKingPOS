using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;

namespace TechKingPOS.App.Data
{
            public static class DiscountRangeRepository
            {
            public static List<DiscountRange> GetAll()
        {
            var list = new List<DiscountRange>();

            using var conn = new SqliteConnection(DbService.ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                @"SELECT Id, FromAmount, ToAmount, DiscountPercent, DiscountAmount
                FROM DiscountRanges
                ORDER BY FromAmount ASC";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new DiscountRange
                {
                    Id = reader.GetInt32(0),
                    From = reader.GetDecimal(1),
                    To = reader.GetDecimal(2),
                    
                    // Use null if DB value is NULL
                    DiscountPercent = reader.IsDBNull(3) ? (decimal?)null : reader.GetDecimal(3),
                    DiscountAmount  = reader.IsDBNull(4) ? (decimal?)null : reader.GetDecimal(4)
                });
            }

            return list;
        }


        public static void SaveAll(List<DiscountRange> ranges)
        {
            using var conn = new SqliteConnection(DbService.ConnectionString);
            conn.Open();

            using var tx = conn.BeginTransaction();

            using (var clear = conn.CreateCommand())
            {
                clear.CommandText = "DELETE FROM DiscountRanges";
                clear.ExecuteNonQuery();
            }

            foreach (var r in ranges)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    @"INSERT INTO DiscountRanges
                      (FromAmount, ToAmount, DiscountPercent, DiscountAmount)
                      VALUES ($f, $t, $p, $a)";

                cmd.Parameters.AddWithValue("$f", r.From);
                cmd.Parameters.AddWithValue("$t", r.To);
                cmd.Parameters.AddWithValue("$p", (object?)r.DiscountPercent ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$a", (object?)r.DiscountAmount ?? DBNull.Value);

                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
    }
}
