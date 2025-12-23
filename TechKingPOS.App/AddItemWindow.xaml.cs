using System;
using System.Collections.Generic;
using System.Windows;
using TechKingPOS.App.Services;
using TechKingPOS.App.Data;
using TechKingPOS.App.Models;
using System.IO;
using System.Linq;
using ExcelDataReader;
using System.Data;
using System.Windows.Controls;

namespace TechKingPOS.App
{     
    public partial class AddItemWindow : Window
    {
          private List<ItemLookup> _allItems = new();
        private bool _suppressTextChange;

        public AddItemWindow()
        {
            InitializeComponent();

            _allItems = ItemRepository.GetAllItems();

            NameBox.TextChanged += ItemSearch_TextChanged;
            AliasBox.TextChanged += ItemSearch_TextChanged;
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
        MessageBox.Show(errorMessage, "Validation Error");
        return;
    }

    string name = NameBox.Text.Trim();
    string alias = AliasBox.Text.Trim();
    int qty = int.Parse(QuantityBox.Text);
    decimal marked = decimal.Parse(MarkedPriceBox.Text);
    decimal selling = decimal.Parse(SellingPriceBox.Text);

    try
    {
        // 🔍 CHECK IF ITEM EXISTS
        var existing = ItemRepository.GetByNameOrAlias(name, alias);

        if (existing != null)
        {
            // ➕ ADD STOCK INSTEAD OF INSERT
            ItemRepository.AddStock(
                existing.Id,
                qty,
                marked,
                selling
            );

            MessageBox.Show(
                $"Stock updated for '{existing.Name}'",
                "Item Updated",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        else
        {
            // 🆕 INSERT NEW ITEM (OLD BEHAVIOR)
            ItemRepository.InsertItem(
                name,
                alias,
                marked,
                selling,
                qty,
                UnitBox.Text.Trim()
            );

            MessageBox.Show(
                "Item added successfully!",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        Clear_Click(null, null);
    }
    catch (Exception ex)
    {
        MessageBox.Show(
            ex.Message,
            "Save Failed",
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

                        private void Upload_Click(object sender, RoutedEventArgs e)
                {
                    var dialog = new Microsoft.Win32.OpenFileDialog
                    {
                        Filter =
                            "All Supported|*.xlsx;*.csv|" +
                            "Excel Files|*.xlsx|" +
                            "CSV Files|*.csv"
                    };

                    if (dialog.ShowDialog() != true)
                        return;

                    string path = dialog.FileName;
                    string ext = Path.GetExtension(path).ToLower();

                    List<ItemImportResult> rows = ext switch
                    {
                        ".csv" => ImportPreviewWindow.FromCsv(path),
                        ".xlsx" => ImportPreviewWindow.FromExcel(path),
                        _ => new List<ItemImportResult>()
                    };

                    if (rows.Count == 0)
                    {
                        MessageBox.Show("No data found in file");
                        return;
                    }

                    var preview = new ImportPreviewWindow(rows);
                   preview.ShowDialog();
                }


        private List<ItemModel> ImportFromCsv(string path)
        {
            var items = new List<ItemModel>();

            foreach (var line in File.ReadAllLines(path).Skip(1))
            {
                var parts = line.Split(',');

                if (parts.Length < 6)
                    continue;

                items.Add(new ItemModel
                {
                    Name = parts[0].Trim(),
                    Alias = parts[1].Trim(),
                    MarkedPrice = decimal.Parse(parts[2]),
                    SellingPrice = decimal.Parse(parts[3]),
                    Quantity = int.Parse(parts[4]),
                    Unit = parts[5].Trim()
                });
            }

            return items;
        }

        private List<ItemModel> ImportFromExcel(string path)
        {
            var items = new List<ItemModel>();

            // Required for ExcelDataReader
            System.Text.Encoding.RegisterProvider(
                System.Text.CodePagesEncodingProvider.Instance);

            using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var dataSet = reader.AsDataSet();
            var table = dataSet.Tables[0];

            for (int row = 1; row < table.Rows.Count; row++) // skip header
            {
                var r = table.Rows[row];

                items.Add(new ItemModel
                {
                    Name = r[0]?.ToString(),
                    Alias = r[1]?.ToString(),
                    MarkedPrice = decimal.Parse(r[2].ToString()),
                    SellingPrice = decimal.Parse(r[3].ToString()),
                    Quantity = int.Parse(r[4].ToString()),
                    Unit = r[5]?.ToString()
                });
            }

            return items;
        }

        private void ItemSearch_TextChanged(object sender, TextChangedEventArgs e)
{
    if (_suppressTextChange)
        return;

    string nameText = NameBox.Text.Trim().ToLower();
    string aliasText = AliasBox.Text.Trim().ToLower();

    if (string.IsNullOrEmpty(nameText) && string.IsNullOrEmpty(aliasText))
    {
        ItemSuggestions.Visibility = Visibility.Collapsed;
        return;
    }

    var matches = _allItems
        .Where(i =>
            (!string.IsNullOrEmpty(nameText) &&
             i.Name.ToLower().Contains(nameText)) ||
            (!string.IsNullOrEmpty(aliasText) &&
             !string.IsNullOrEmpty(i.Alias) &&
             i.Alias.ToLower().Contains(aliasText)))
        .ToList();

    if (matches.Count == 0)
    {
        ItemSuggestions.Visibility = Visibility.Collapsed;
        return;
    }

    ItemSuggestions.ItemsSource = matches;
    ItemSuggestions.Visibility = Visibility.Visible;
}
    private void ItemSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (ItemSuggestions.SelectedItem is not ItemLookup item)
        return;

    _suppressTextChange = true;

    NameBox.Text = item.Name;
    AliasBox.Text = item.Alias;
    MarkedPriceBox.Text = item.MarkedPrice.ToString("0.00");
    SellingPriceBox.Text = item.SellingPrice.ToString("0.00");
    UnitBox.Text = item.Unit;

    QuantityBox.Clear(); // ❌ DO NOT AUTO-FILL

    _suppressTextChange = false;

    ItemSuggestions.Visibility = Visibility.Collapsed;
    ItemSuggestions.SelectedItem = null;
}


    }
}
