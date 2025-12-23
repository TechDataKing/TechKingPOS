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

        public static readonly string ConnectionString =
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
        Balance REAL NOT NULL DEFAULT 0,
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

    -- Customers are unique by Name (phone can be NULL)
    CREATE TABLE IF NOT EXISTS Customers (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Name TEXT NOT NULL UNIQUE,
        Phone TEXT
    );

    CREATE TABLE IF NOT EXISTS Payments (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        ReceiptNumber TEXT NOT NULL,
        Method TEXT NOT NULL,
        Amount REAL NOT NULL,
        CreatedAt TEXT NOT NULL
    );

    -- Credits are CUSTOMER-BASED but still receipt-linked
    CREATE TABLE IF NOT EXISTS Credits (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        CustomerId INTEGER NOT NULL UNIQUE,
        ReceiptNumber TEXT NOT NULL,
        Total REAL NOT NULL,
        Paid REAL NOT NULL,
        Balance REAL NOT NULL,
        CreatedAt TEXT NOT NULL,
        FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
    );
        
    CREATE TABLE IF NOT EXISTS CreditPayments (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        CreditId INTEGER NOT NULL,
        Amount REAL NOT NULL,
        Method TEXT NOT NULL,
        CreatedAt TEXT NOT NULL,
        FOREIGN KEY (CreditId) REFERENCES Credits(Id)
    );

    CREATE TABLE IF NOT EXISTS Workers (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Name TEXT NOT NULL,
        NationalId TEXT NOT NULL,
        Phone TEXT NOT NULL,
        Email TEXT,
        PasswordHash TEXT NOT NULL,
        IsActive INTEGER NOT NULL DEFAULT 1,
        CreatedAt TEXT NOT NULL
    );

    CREATE TABLE IF NOT EXISTS AppSettings (
        Id INTEGER PRIMARY KEY,
        BusinessName TEXT,
        Phone TEXT,
        ReceiptFooter TEXT,
        VatEnabled INTEGER,
        VatPercent REAL,
        RequireLogin INTEGER,
        LicenseKey TEXT,
        LicenseExpiry TEXT
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
