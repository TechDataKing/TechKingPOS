using System;
using System.IO;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Services;

namespace TechKingPOS.App.Data
{
    public static class DbService
    {
        // ✅ INSTALLER-SAFE, WRITABLE LOCATION
        private static readonly string DbPath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TechKingPOS",
                "Data",
                "app.db"
            );

        public static readonly string ConnectionString =
            $"Data Source={DbPath};Pooling=True;";

static DbService()
{
    try
    {
        EnsureDatabase();
    }
    catch (Exception ex)
    {
        try
        {
            var baseDir = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            );

            var logDir = Path.Combine(baseDir, "TechKingPOS", "crash");
            Directory.CreateDirectory(logDir);

            var logFile = Path.Combine(
                logDir,
                "DB_STATIC_INIT_ERROR.txt"
            );

            File.WriteAllText(
                logFile,
                "=== DbService STATIC INIT FAILURE ===\n\n" +
                ex.ToString()
            );
        }
        catch
        {
            // absolutely swallow — we must not hide original error
        }

        throw; // 🔥 still crash — but now we KNOW WHY
    }
}


        private static void EnsureDatabase()
        {
            // ✅ Ensure folders exist
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            ApplyPragmas(connection);
            CreateTables(connection);
            EnsureDefaultBranch(connection);

            LoggerService.Info("🗄️", "DB", "Database initialized", DbPath);
        }

        private static void ApplyPragmas(SqliteConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = NORMAL;
                PRAGMA busy_timeout = 5000;
                PRAGMA foreign_keys = ON;
            ";
            cmd.ExecuteNonQuery();
        }

        // ⚠️ YOUR TABLES — UNCHANGED
        private static void CreateTables(SqliteConnection connection)
        {
            var cmd = connection.CreateCommand();

            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Branch (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    Code TEXT NOT NULL UNIQUE,
    IsActive INTEGER DEFAULT 1,
    CreatedAt TEXT NOT NULL
);
    CREATE TABLE IF NOT EXISTS Items (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        BranchId INTEGER NOT NULL DEFAULT 1,
        Name TEXT NOT NULL,
        Alias TEXT,
        MarkedPrice REAL NOT NULL,
        SellingPrice REAL NOT NULL,
        Quantity REAL NOT NULL,
        UnitType TEXT NOT NULL DEFAULT 1,
        UnitValUE REAL NULL,
        CreatedAt TEXT NOT NULL,
        TargetQuantity INTEGER,
        FOREIGN KEY (BranchId) REFERENCES Branch(Id)
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
        BranchId INTEGER NOT NULL DEFAULT 1,
        CreatedAt TEXT NOT NULL,
        FOREIGN KEY (BranchId) REFERENCES Branch(Id)    
    );

    CREATE TABLE IF NOT EXISTS Sales (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        ReceiptNumber TEXT NOT NULL,
        ItemId TEXT NOT NULL,
        ItemName TEXT NOT NULL,
        UnitType TEXT NOT NULL DEFAULT 1,
        UnitValUE REAL NULL,
        Quantity INTEGER NOT NULL,
        Price REAL NOT NULL,
        Total REAL NOT NULL,
        CostPrice REAL NOT NULL DEFAULT 0,
        Profit REAL NOT NULL DEFAULT 0,
        BranchId INTEGER NOT NULL DEFAULT 1,
        CreatedAt TEXT NOT NULL,
        FOREIGN KEY (BranchId) REFERENCES Branch(Id)
    );

    -- Customers are unique by Name (phone can be NULL)
    CREATE TABLE IF NOT EXISTS Customers (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Name TEXT NOT NULL ,
        Phone TEXT,
        BranchId INTEGER NOT NULL DEFAULT 1,
        UNIQUE (Name , BranchId),
        FOREIGN KEY (BranchId) REFERENCES Branch(Id)
    );

    CREATE TABLE IF NOT EXISTS Payments (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        ReceiptNumber TEXT NOT NULL,
        Method TEXT NOT NULL,
        Amount REAL NOT NULL,
        BranchId INTEGER NOT NULL DEFAULT 1,
        CreatedAt TEXT NOT NULL,
        FOREIGN KEY (BranchId) REFERENCES Branch(Id)
    );

    -- Credits are CUSTOMER-BASED but still receipt-linked
    CREATE TABLE IF NOT EXISTS Credits (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        CustomerId INTEGER NOT NULL UNIQUE,
        ReceiptNumber TEXT NOT NULL,
        Total REAL NOT NULL,
        Paid REAL NOT NULL,
        Balance REAL NOT NULL,
        BranchId INTEGER NOT NULL DEFAULT 1,
        CreatedAt TEXT NOT NULL,
        FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
        FOREIGN KEY (BranchId) REFERENCES Branch(Id)
    );
        
    CREATE TABLE IF NOT EXISTS CreditPayments (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        CreditId INTEGER NOT NULL,
        Amount REAL NOT NULL,
        Method TEXT NOT NULL,
        BranchId INTEGER NOT NULL DEFAULT 1,
        CreatedAt TEXT NOT NULL,
        FOREIGN KEY (CreditId) REFERENCES Credits(Id),
        FOREIGN KEY (BranchId) REFERENCES Branch(Id)
    );

CREATE TABLE IF NOT EXISTS Workers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    NationalId TEXT NOT NULL,
    Phone TEXT NOT NULL,
    Email TEXT,
    PasswordHash TEXT NOT NULL,
    Role INTEGER NOT NULL DEFAULT 2,
    IsActive INTEGER NOT NULL DEFAULT 0,
    MustChangePassword INTEGER  DEFAULT 1,
     BranchId INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL
);



    CREATE TABLE IF NOT EXISTS DamagedItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ItemId INTEGER NOT NULL,
    ItemName TEXT NOT NULL,
    Alias TEXT,
    UnitType TEXT,
    Quantity INTEGER NOT NULL,
    MarkedPrice REAL NOT NULL,
    SellingPrice REAL NOT NULL,
    Reason TEXT NOT NULL,
    RecordedBy TEXT NOT NULL,
    BranchId INTEGER NOT NULL DEFAULT 1,
    DamagedAt TEXT NOT NULL,
    FOREIGN KEY (ItemId) REFERENCES Items(Id),
    FOREIGN KEY (BranchId) REFERENCES Branch(Id)
);

CREATE TABLE IF NOT EXISTS Expenses (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Date TEXT NOT NULL,
    Category TEXT NOT NULL,
    Description TEXT,
    Amount REAL NOT NULL,
    PaymentMethod TEXT NOT NULL,
    BranchId INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (BranchId) REFERENCES Branch(Id)
);




    CREATE TABLE IF NOT EXISTS AppSettings
(
    Id INTEGER PRIMARY KEY,

    BusinessName TEXT,
    BranchName TEXT,
    Phone TEXT,
    Email TEXT,
    PhysicalAddress TEXT,
    ReceiptFooter TEXT,

    AutoPrintReceipt INTEGER,
    ShowCashierOnReceipt INTEGER,
    ShowLogoOnReceipt INTEGER,
    PaperSize TEXT,
    ReceiptCopies INTEGER,

    AllowNegativeStock INTEGER,
    AllowPriceEditDuringSale INTEGER,
    EnableCreditSales INTEGER,

    VatEnabled INTEGER,
    VatPercent REAL,

    RequireLogin INTEGER,
    AutoLogout INTEGER,
    AllowVoidSales INTEGER,
    AllowWorkersEditPrices INTEGER,
    AllowWorkersGiveDiscounts INTEGER,

    LicenseKey TEXT,
    LicenseExpiry TEXT,

    EnableDiscounts INTEGER,

    AllowFixedDiscount INTEGER,
    AllowPercentageDiscount INTEGER,
    AllowConditionalDiscount INTEGER,

    MaxFixedDiscount REAL,
    MaxPercentageDiscount REAL,

    CondValueFixed INTEGER,
    CondValuePercent INTEGER,
    CondBasedRanges INTEGER,

    ConditionalMinSubtotal REAL,
    ConditionalDiscountAmount REAL
);



          CREATE TABLE IF NOT EXISTS DiscountRanges (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            FromAmount REAL NOT NULL,
            ToAmount REAL NOT NULL,
            DiscountPercent REAL NULL,
            DiscountAmount REAL NULL
        );


CREATE TABLE IF NOT EXISTS Activity (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            -- What was affected
    EntityType TEXT NOT NULL,      -- e.g. Items, Sales, Payments
    EntityId INTEGER,              -- nullable for global/system actions
    EntityName TEXT,
                                     -- What happened
    Action TEXT NOT NULL,          -- INSERT, UPDATE, SALE, DAMAGE, ADD_STOCK
                                       -- Stock impact (BASE UNIT ONLY)
    QuantityChange REAL NOT NULL DEFAULT 0,
    UnitType TEXT,                 -- kg / l / pieces (display only)
    UnitValue REAL,
    Price REAL,                     -- conversion context
                                     -- Optional snapshots (future-safe)
    BeforeValue TEXT,              -- JSON (nullable)
    AfterValue TEXT,               -- JSON (nullable)
                                         -- Meta
    Reason TEXT,                   -- human explanation
    PerformedBy TEXT NOT NULL,     -- user / SYSTEM
    BranchId INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS RepackRules (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,

    -- Source item (bulk)
    ItemId INTEGER NOT NULL,
    ItemName TEXT NOT NULL,

    -- How it is sold
    UnitValue REAL NOT NULL,         -- e.g. 0.25
    UnitType TEXT NOT NULL,         -- kg / g / ml / l / pieces

    SellingPrice REAL NOT NULL,

    -- Status
    IsActive INTEGER NOT NULL DEFAULT 1,

    CreatedAt TEXT NOT NULL,

    FOREIGN KEY (ItemId) REFERENCES Items(Id)
);

CREATE TABLE IF NOT EXISTS permissions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    key TEXT NOT NULL UNIQUE,
    description TEXT
);

CREATE TABLE IF NOT EXISTS roles (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE
);
CREATE TABLE IF NOT EXISTS role_permissions (
    role_id INTEGER NOT NULL,
    permission_id INTEGER NOT NULL,
    PRIMARY KEY (role_id, permission_id)
);
CREATE TABLE IF NOT EXISTS user_roles (
    user_id INTEGER NOT NULL,
    role_id INTEGER NOT NULL,
    PRIMARY KEY (user_id, role_id)
);
CREATE TABLE IF NOT EXISTS user_permissions (
    user_id INTEGER NOT NULL,
    permission_key TEXT NOT NULL,
    granted INTEGER NOT NULL DEFAULT 1,
    granted_at TEXT NOT NULL,

    PRIMARY KEY (user_id, permission_key)
);




";
            cmd.ExecuteNonQuery();
        }

        private static void EnsureDefaultBranch(SqliteConnection connection)
        {
            using var check = connection.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM Branch;";
            long count = (long)check.ExecuteScalar();

            if (count > 0)
                return;

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Branch (Name, Code, IsActive, CreatedAt)
                VALUES ('Main Branch', 'MAIN', 1, @created);
            ";

            cmd.Parameters.AddWithValue(
                "@created",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            );

            cmd.ExecuteNonQuery();
        }

        public static SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();

            return conn;
        }
    }
}
