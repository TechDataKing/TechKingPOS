using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using TechKingPOS.App.Models;
using TechKingPOS.App.Data;
using TechKingPOS.App.Services;
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

    var skipped = new List<ItemImportResult>();
    var imported = 0;

    foreach (var r in validRows)
    {
        decimal qty = decimal.Parse(
            r.Quantity,
            System.Globalization.CultureInfo.InvariantCulture
        );

        decimal marked = decimal.Parse(
            r.MarkedPrice,
            System.Globalization.CultureInfo.InvariantCulture
        );

        decimal selling = decimal.Parse(
            r.SellingPrice,
            System.Globalization.CultureInfo.InvariantCulture
        );

        decimal unitValue =
            r.UnitType == "pieces"
                ? 1m
                : decimal.Parse(
                    r.UnitValue,
                    System.Globalization.CultureInfo.InvariantCulture
                );

        // 🔄 SAFE UNIT CONVERSION
        if (!UnitConverter.TryToBase(
                r.UnitType,
                qty,
                unitValue,
                out var baseUnit,
                out var baseQty,
                out var unitError))
        {
            r.IsValid = false;
            r.Error = unitError;
            skipped.Add(r);
            continue;
        }

        // 🔍 CHECK EXISTING ITEM
        string cleanName = r.Name.Trim();
        string? cleanAlias =
            string.IsNullOrWhiteSpace(r.Alias) ? null : r.Alias.Trim();

        var existing = ItemRepository.GetByNameOrAlias(cleanName, cleanAlias);

        if (existing != null)
        {
            if (existing.UnitType != baseUnit)
            {
                r.IsValid = false;
                r.Error =
                    $"Unit mismatch: existing [{existing.UnitType}], import [{baseUnit}]. Confirm unit before importing.";
                skipped.Add(r);
                continue;
            }

            ItemRepository.AddStock(
                existing.Id,
                baseQty,
                marked,
                selling
            );
        }
        else
        {
            ItemRepository.InsertItem(
                r.Name,
                r.Alias,
                marked,
                selling,
                baseQty,
                baseUnit,
                unitValue
            );
        }

        imported++;
    }

    // 📢 SUMMARY MESSAGE
    var message =
        $"Imported: {imported} items\n" +
        $"Skipped: {skipped.Count} items";

    if (skipped.Any())
    {
        message += "\n\nSkipped items:\n" +
            string.Join(
                "\n",
                skipped.Select(s => $"- {s.Name}: {s.Error}")
            );
    }

    MessageBox.Show(
        message,
        "Import Result",
        MessageBoxButton.OK,
        skipped.Any()
            ? MessageBoxImage.Warning
            : MessageBoxImage.Information
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
                    parts.ElementAtOrDefault(5),
                    parts.ElementAtOrDefault(6)
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
                    r[5]?.ToString(),
                    r[6]?.ToString()
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
            string unitType,
            string unitValue)
        {
            var row = new ItemImportResult
            {
                Name = name?.Trim(),
                Alias = alias?.Trim(),
                MarkedPrice = marked?.Trim(),
                SellingPrice = selling?.Trim(),
                Quantity = qty?.Trim(),
                UnitType = unitType?.Trim(),
                UnitValue = unitValue?.Trim()

            };

            try
            {
                decimal.Parse(row.MarkedPrice);
                decimal.Parse(row.SellingPrice);
                decimal.Parse(row.Quantity);

                if (row.UnitType != "pieces")
                {
                    if (string.IsNullOrWhiteSpace(row.UnitValue))
                        throw new Exception("UnitValue required");

                    decimal.Parse(row.UnitValue);
                }

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
