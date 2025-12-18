using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechKingPOS.App.Models;

namespace TechKingPOS.App.Services
{
    public static class ReceiptBuilder
    {
        private const int Width = 32;
        private const int MinReceiptLines = 26; // minimum total height
        private const int FooterLines = 2;      // footer line count

        public static string Build(
            IEnumerable<SaleItem> items,
            decimal amountPaid,
            string cashier = "Admin")
        {
            var sb = new StringBuilder();

            decimal total = items.Sum(i => i.Total);
            decimal subtotal = Math.Round(total / 1.16m, 2);
            decimal vat = total - subtotal;
            int totalQty = items.Sum(i => i.Quantity);

            // HEADER
            sb.AppendLine("Quality Electronics");
            sb.AppendLine("Tel: 0712 345 678");
            sb.AppendLine(Line());

            sb.AppendLine($"Receipt #: {DateTime.Now:yyyyMMddHHmmss}");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Cashier: {cashier}");
            sb.AppendLine(Line());

            // ITEMS HEADER
            sb.AppendLine("ITEM            QTY   PRICE   TOTAL");
            sb.AppendLine(Line());

            foreach (var i in items)
            {
                sb.AppendLine(
                    $"{Trim(i.Name, 15),-15}" +
                    $"{i.Quantity,5}" +
                    $"{i.Price,8:0}" +
                    $"{i.Total,8:0}"
                );
            }

            sb.AppendLine(Line());

            // TOTALS
            sb.AppendLine($"Items:{totalQty,22}");
            sb.AppendLine($"Subtotal:{subtotal,18:0.00}");
            sb.AppendLine($"VAT (16%):{vat,18:0.00}");
            sb.AppendLine(Line());
            sb.AppendLine($"TOTAL:{total,22:0.00}");
            sb.AppendLine(Line());

            // PAYMENT
            sb.AppendLine("Payment: CASH");
            sb.AppendLine($"Amount Paid:{amountPaid,15:0.00}");
            sb.AppendLine($"Change:{(amountPaid - total),20:0.00}");
            sb.AppendLine(Line());

            // ===== FORCE MIN HEIGHT (RESERVE FOOTER SPACE) =====
            int contentLines = sb
                .ToString()
                .Split('\n', StringSplitOptions.None)
                .Length;

            int targetLines = MinReceiptLines - FooterLines;

            while (contentLines < targetLines)
            {
                sb.AppendLine();
                contentLines++;
            }

            // ===== FOOTER (ALWAYS VISUALLY BOTTOM) =====
            sb.AppendLine("Thank you for shopping with us");
            sb.AppendLine(Center("© TechKing"));

            return sb.ToString();
        }

        private static string Line() => new string('-', Width);

        private static string Center(string text)
        {
            int pad = Math.Max(0, (Width - text.Length) / 2);
            return new string(' ', pad) + text;
        }

        private static string Trim(string text, int max)
            => string.IsNullOrEmpty(text)
                ? string.Empty
                : text.Length <= max ? text : text[..max];
    }
}
