using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace TechKingPOS.App.Data
{
    public static class LogDb
    {
        private static readonly string DbPath =
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "logs",
                "logs.db"
            );

        public static SqliteConnection GetConnection()
        {
            var dir = Path.GetDirectoryName(DbPath);

            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return new SqliteConnection($"Data Source={DbPath}");
        }

        public static void Initialize()
        {
            using var conn = GetConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS logs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT,
                Level TEXT,
                Emoji TEXT,
                Category TEXT,
                Message TEXT,
                Details TEXT
            );
            ";
            cmd.ExecuteNonQuery();
        }
    }
}
