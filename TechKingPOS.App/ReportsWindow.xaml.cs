using System;
using System.Windows;
using System.Windows.Controls;
using TechKingPOS.App.Data;

namespace TechKingPOS.App
{
    public partial class ReportsWindow : Window
    {
        private bool _isLoaded = false;

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
    }
}
