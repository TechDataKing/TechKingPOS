using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using TechKingPOS.App.Models;

using TechKingPOS.App.Data;
using TechKingPOS.App.Services;


namespace TechKingPOS.App
{
    public partial class ReportsWindow : Window
    {
        private bool _isLoaded = false;
         private List<CreditView> _allCredits = new();

        public ReportsWindow()
        {
            InitializeComponent();

            // DO NOT call load methods here
            Loaded += ReportsWindow_Loaded;
        }

        // ================= WINDOW LOADED =================
        private void ReportsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;

            // SALES DEFAULT
            LoadToday();

            // INVENTORY DEFAULT
            LoadOutOfStock();

            // CREDITS DEFAULT
            LoadCredits();
        }

        // ================= SALES =================

        private void LoadToday()
        {
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;
            LoadReport();
        }

        private void LoadReport_Click(object sender, RoutedEventArgs e)
        {
            LoadReport();
        }

        private void LoadReport()
        {
            if (FromDatePicker.SelectedDate == null ||
                ToDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Select date range.");
                return;
            }

            DateTime from = FromDatePicker.SelectedDate.Value.Date;
            DateTime to = ToDatePicker.SelectedDate.Value
                .Date
                .AddDays(1)
                .AddSeconds(-1);

            var summary = ReportsRepository.GetSalesSummary(from, to);

            TotalSalesText.Text = summary.TotalSales.ToString("0.00");
            ProfitText.Text = summary.Profit.ToString("0.00");
            ReceiptCountText.Text = summary.ReceiptCount.ToString();

            SoldItemsGrid.ItemsSource =
                ReportsRepository.GetSoldItems(from, to);
        }

        // ================= INVENTORY =================

        private void InventoryFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            // 🔒 CRITICAL GUARD
            if (!_isLoaded)
                return;

            if (InventoryFilterCombo.SelectedItem is not ComboBoxItem item)
                return;

            string filter = item.Content.ToString();

            switch (filter)
            {
                case "Out of Stock":
                    LoadOutOfStock();
                    break;

                case "Running Low":
                    LoadRunningLow();
                    break;

                case "Good Stock":
                    LoadGoodStock();
                    break;
            }
        }

        private void LoadOutOfStock()
        {
            InventoryGrid.ItemsSource =
                InventoryRepository.GetOutOfStock();
        }

        private void LoadRunningLow()
        {
            InventoryGrid.ItemsSource =
                InventoryRepository.GetRunningLow();
        }

        private void LoadGoodStock()
        {
            InventoryGrid.ItemsSource =
                InventoryRepository.GetGoodStock();
        }
        // ================= CREDITS =================  
        private void LoadCredits()
{
    _allCredits.Clear();
    decimal totalCredit = 0;

    using var conn = DbService.GetConnection();
    conn.Open();

    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        SELECT
            c.Id,                    -- 0 CreditId
            cust.Name,               -- 1 CustomerName
            cust.Phone,              -- 2 Phone
            c.Total,                 -- 3 Total
            c.Paid,                  -- 4 Paid
            c.Balance,               -- 5 Balance
            IFNULL(
                (
                    SELECT MAX(CreatedAt)
                    FROM CreditPayments cp
                    WHERE cp.CustomerId = c.CustomerId
                ),
                c.CreatedAt
            )                         -- 6 LastPaymentDate
        FROM Credits c
        JOIN Customers cust ON cust.Id = c.CustomerId
        WHERE c.Balance > 0
        ORDER BY c.CreatedAt DESC;
    ";

    using var reader = cmd.ExecuteReader();

    while (reader.Read())
    {
        decimal balance = reader.GetDecimal(5);
        DateTime lastPayment =
            DateTime.Parse(reader.GetString(6));

        totalCredit += balance;

        _allCredits.Add(new CreditView
        {
            CreditId = reader.GetInt32(0),
            CustomerName = reader.GetString(1),
            Phone = reader.IsDBNull(2) ? "" : reader.GetString(2),
            Total = reader.GetDecimal(3),
            Paid = reader.GetDecimal(4),
            Balance = balance,
            LastPaymentDate = lastPayment
            // ✅ DaysSinceLastPayment & Status auto-computed
        });
    }

    CreditGrid.ItemsSource = _allCredits;
    TotalCreditText.Text = totalCredit.ToString("0.00");
}


// ================= CREDIT SEARCH =================
private void CreditSearchChanged(object sender, TextChangedEventArgs e)
{
    string text = CreditSearchTextBox.Text.Trim().ToLower();

    if (string.IsNullOrEmpty(text))
    {
        CreditGrid.ItemsSource = _allCredits;
        return;
    }

    CreditGrid.ItemsSource = _allCredits.Where(c =>
        c.CustomerName.ToLower().Contains(text) ||
        c.Phone.ToLower().Contains(text)
    ).ToList();
}



    }
}