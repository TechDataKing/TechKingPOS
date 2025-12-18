using System;
using System.IO;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Services;

namespace TechKingPOS.App.Data
{
    public static class DbService
    {
        private static readonly string DbPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "app.db");

        private static readonly string ConnectionString =
            $"Data Source={DbPath}";

        static DbService()
        {
            EnsureDatabase();
        }

        private static void EnsureDatabase()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            LoggerService.Info("🗄️", "DB", "Database initialized", DbPath);

            CreateTables(connection);
        }

       private static void CreateTables(SqliteConnection connection)
{
    var cmd = connection.CreateCommand();

    cmd.CommandText = @"
    CREATE TABLE IF NOT EXISTS Items (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Name TEXT NOT NULL,
        Alias TEXT,
        MarkedPrice REAL NOT NULL,
        SellingPrice REAL NOT NULL,
        Quantity INTEGER NOT NULL,
        Unit TEXT NOT NULL,
        CreatedAt TEXT NOT NULL,
        TargetQuantity INTEGER
    );

    CREATE TABLE IF NOT EXISTS Transactions (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        ReceiptNumber TEXT NOT NULL UNIQUE,
        Cashier TEXT NOT NULL,
        Total REAL NOT NULL,
        Tax REAL NOT NULL,
        Discount REAL NOT NULL,
        AmountPaid REAL NOT NULL,
        CreatedAt TEXT NOT NULL
    );

    CREATE TABLE IF NOT EXISTS Sales (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        ReceiptNumber TEXT NOT NULL,
        ItemName TEXT NOT NULL,
        Quantity INTEGER NOT NULL,
        Price REAL NOT NULL,
        Total REAL NOT NULL,
        CreatedAt TEXT NOT NULL
    );
    ";

    cmd.ExecuteNonQuery();
}



        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(ConnectionString);
        }
    }
}
