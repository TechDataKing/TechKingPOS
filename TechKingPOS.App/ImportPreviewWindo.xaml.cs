using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using TechKingPOS.App.Models;
using TechKingPOS.App.Data;
using ExcelDataReader;
using System.Data;

namespace TechKingPOS.App
{
    public partial class ImportPreviewWindow : Window
    {
        private readonly List<ItemImportResult> _rows;

        public ImportPreviewWindow(List<ItemImportResult> rows)
        {
            InitializeComponent();
            _rows = rows;
            DataContext = rows;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var validRows = _rows.Where(r => r.IsValid).ToList();

            foreach (var r in validRows)
            {
                ItemRepository.InsertItem(
                    r.Name,
                    r.Alias,
                    decimal.Parse(r.MarkedPrice),
                    decimal.Parse(r.SellingPrice),
                    int.Parse(r.Quantity),
                    r.Unit
                );
            }

            MessageBox.Show(
                $"{validRows.Count} items imported successfully",
                "Import Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            Close();
        }

        // ---------- CSV ----------
        public static List<ItemImportResult> FromCsv(string path)
        {
            var results = new List<ItemImportResult>();

            foreach (var line in File.ReadAllLines(path).Skip(1))
            {
                var parts = line.Split(',');

                var row = BuildRow(
                    parts.ElementAtOrDefault(0),
                    parts.ElementAtOrDefault(1),
                    parts.ElementAtOrDefault(2),
                    parts.ElementAtOrDefault(3),
                    parts.ElementAtOrDefault(4),
                    parts.ElementAtOrDefault(5)
                );

                results.Add(row);
            }

            return results;
        }

        // ---------- EXCEL ----------
        public static List<ItemImportResult> FromExcel(string path)
        {
            var results = new List<ItemImportResult>();

            System.Text.Encoding.RegisterProvider(
                System.Text.CodePagesEncodingProvider.Instance);

            using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var table = reader.AsDataSet().Tables[0];

            for (int i = 1; i < table.Rows.Count; i++) // skip header
            {
                var r = table.Rows[i];

                var row = BuildRow(
                    r[0]?.ToString(),
                    r[1]?.ToString(),
                    r[2]?.ToString(),
                    r[3]?.ToString(),
                    r[4]?.ToString(),
                    r[5]?.ToString()
                );

                results.Add(row);
            }

            return results;
        }

        // ---------- VALIDATION ----------
        private static ItemImportResult BuildRow(
            string name,
            string alias,
            string marked,
            string selling,
            string qty,
            string unit)
        {
            var row = new ItemImportResult
            {
                Name = name?.Trim(),
                Alias = alias?.Trim(),
                MarkedPrice = marked?.Trim(),
                SellingPrice = selling?.Trim(),
                Quantity = qty?.Trim(),
                Unit = unit?.Trim()
            };

            try
            {
                decimal.Parse(row.MarkedPrice);
                decimal.Parse(row.SellingPrice);
                int.Parse(row.Quantity);

                row.IsValid = true;
                row.Error = "OK";
            }
            catch
            {
                row.IsValid = false;
                row.Error = "Invalid data";
            }

            return row;
        }
    }
}
