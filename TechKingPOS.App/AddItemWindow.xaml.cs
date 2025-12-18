using System;
using System.Collections.Generic;
using System.Windows;
using TechKingPOS.App.Services;
using TechKingPOS.App.Data;

namespace TechKingPOS.App
{
    public partial class AddItemWindow : Window
    {
        public AddItemWindow()
        {
            InitializeComponent();

            LoggerService.Info(
                "🧾",
                "UI",
                "Add Item window opened"
            );
        }

        private bool ValidateForm(out string errorMessage)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(NameBox.Text))
                errors.Add("• Item Name is required");

            if (string.IsNullOrWhiteSpace(MarkedPriceBox.Text) ||
                !decimal.TryParse(MarkedPriceBox.Text, out _))
                errors.Add("• Marked Price must be a valid number");

            if (string.IsNullOrWhiteSpace(SellingPriceBox.Text) ||
                !decimal.TryParse(SellingPriceBox.Text, out _))
                errors.Add("• Selling Price must be a valid number");

            if (string.IsNullOrWhiteSpace(QuantityBox.Text) ||
                !int.TryParse(QuantityBox.Text, out _))
                errors.Add("• Quantity must be a whole number");

            if (string.IsNullOrWhiteSpace(UnitBox.Text))
                errors.Add("• Unit is required (e.g kg, pcs, ltr)");

            errorMessage = string.Join(Environment.NewLine, errors);
            return errors.Count == 0;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm(out var errorMessage))
            {
                LoggerService.Info(
                    "⚠️",
                    "VALIDATION",
                    "Add Item validation failed",
                    errorMessage
                );

                MessageBox.Show(
                    errorMessage,
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            try
            {
                ItemRepository.InsertItem(
                    NameBox.Text.Trim(),
                    AliasBox.Text.Trim(),
                    decimal.Parse(MarkedPriceBox.Text),
                    decimal.Parse(SellingPriceBox.Text),
                    int.Parse(QuantityBox.Text),
                    UnitBox.Text.Trim()
                );

                LoggerService.Info(
                    "💾",
                    "DB",
                    "Item saved successfully",
                    $"Name={NameBox.Text}, Qty={QuantityBox.Text}, Unit={UnitBox.Text}"
                );

                MessageBox.Show(
                    "Item Saved Successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                LoggerService.Error(
                    "❌",
                    "DB",
                    "Failed to save item",
                    ex
                );

                MessageBox.Show(
                    "Failed to save item. Check logs.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            NameBox.Clear();
            AliasBox.Clear();
            MarkedPriceBox.Clear();
            SellingPriceBox.Clear();
            QuantityBox.Clear();
            UnitBox.Clear();

            LoggerService.Info(
                "🧹",
                "UI",
                "Add Item form cleared"
            );
        }
    }
}
