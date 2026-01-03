using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechKingPOS.App.Models;
using TechKingPOS.App.Data;
using TechKingPOS.App.Security;


namespace TechKingPOS.App.Services
{
    public static class ReceiptBuilder
    {
        private static int GetReceiptWidth()
        {
            var size = SettingsCache.Current?.PaperSize;
            return size == "80mm" ? 48 : 32;
        }

        private const int MinReceiptLines = 26;
        private const int FooterLines = 2;

        public static string Build(
            IEnumerable<SaleItem> items,
            PaymentResult payment,
            string receiptNumber,
            string cashier )
        {
            var sb = new StringBuilder();
            int width = GetReceiptWidth();
            decimal totalQty = items.Sum(i => i.Quantity);

// ================= HEADER =================
var settings = SettingsCache.Current;

// Business name
if (!string.IsNullOrWhiteSpace(settings?.BusinessName))
    sb.AppendLine(Trim(settings.BusinessName, width));

        // Address (ONE LINE)
        if (!string.IsNullOrWhiteSpace(settings?.PhysicalAddress))
            sb.AppendLine(Trim(settings.PhysicalAddress, width));

        // Phone
        if (!string.IsNullOrWhiteSpace(settings?.Phone))
            sb.AppendLine($"Tel: {Trim(settings.Phone, width - 5)}");

        sb.AppendLine(Line(width));

        sb.AppendLine($"Receipt #: {receiptNumber}");
        sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");

        // Cashier (FIRST NAME ONLY)
        if (settings?.ShowCashierOnReceipt == true)
        {
            string cashierName = cashier;

            if (!string.IsNullOrWhiteSpace(UserSession.UserName))
            {
                cashierName = UserSession.UserName
                    .Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            }

            sb.AppendLine($"Cashier: {cashierName}");
        }
        sb.AppendLine(Line(width));

                    // ================= ITEMS =================
            sb.AppendLine("ITEM            QTY   PRICE   TOTAL");
            sb.AppendLine(Line(width));

            foreach (var i in items)
            {
                sb.AppendLine(
                    $"{Trim(i.Name, 15),-15}" +
                    $"{i.Quantity,5}" +
                    $"{i.Price,8:0}" +
                    $"{i.Total,8:0}"
                );
            }

            sb.AppendLine(Line(width));

            // ================= TOTALS =================
            sb.AppendLine($"Items:{totalQty,22}");
            sb.AppendLine($"Subtotal:{payment.Subtotal,18:0.00}");

            if (payment.Discount > 0)
                sb.AppendLine($"Discount:{-payment.Discount,18:0.00}");

            if (settings?.VatEnabled == true && payment.Vat > 0)
            {
                sb.AppendLine(
                    $"VAT ({settings.VatPercent:0.#}%):{payment.Vat,18:0.00}"
                );
            }
            sb.AppendLine(Line(width));
            sb.AppendLine($"TOTAL:{payment.Total,22:0.00}");


            sb.AppendLine(Line(width));

            // ================= PAYMENT =================
            BuildPaymentSection(sb, payment);

            sb.AppendLine(Line(width));

            // ===== FORCE MIN HEIGHT =====
            int contentLines = sb
                .ToString()
                .Split('\n', StringSplitOptions.None)
                .Length;

            int targetLines = MinReceiptLines - FooterLines;

            while (contentLines
 < targetLines)
            {
                sb.AppendLine();
                contentLines++;
            }

// ================= FOOTER =================
    if (!string.IsNullOrWhiteSpace(settings?.ReceiptFooter))
    {
        sb.AppendLine(Center(Trim(settings.ReceiptFooter, width),width));
    }
            sb.AppendLine(Center("© TechKing",width));

            return sb.ToString();
        }

        // =========================================================

        private static void BuildPaymentSection(
            StringBuilder sb,
            PaymentResult payment)
        {
            bool hasCash = payment.CashAmount > 0;
            bool hasMpesa = payment.MpesaAmount > 0;

            if (payment.Balance > 0)
            {
                sb.AppendLine("Payment: CREDIT");
                sb.AppendLine($"Paid:{payment.AmountPaid,22:0.00}");
                sb.AppendLine($"Balance:{payment.Balance,19:0.00}");

                if (!string.IsNullOrWhiteSpace(payment.CustomerName))
                    sb.AppendLine($"Customer: {payment.CustomerName}");

                if (!string.IsNullOrWhiteSpace(payment.Phone))
                    sb.AppendLine($"Phone: {payment.Phone}");

                return;
            }

            if (hasCash && hasMpesa)
            {
                sb.AppendLine("Payment: MIXED");
                sb.AppendLine($"Cash:{payment.CashAmount,23:0.00}");
                sb.AppendLine($"Mpesa:{payment.MpesaAmount,21:0.00}");
                sb.AppendLine($"Paid:{payment.AmountPaid,23:0.00}");
                sb.AppendLine($"Change:{(payment.Change),20:0.00}");
                return;
            }

            if (hasMpesa)
            {
                sb.AppendLine("Payment: MPESA");
                sb.AppendLine($"Amount Paid:{payment.AmountPaid,15:0.00}");
                sb.AppendLine("Change: 0.00");
                return;
            }

            // CASH ONLY
            sb.AppendLine("Payment: CASH");
            sb.AppendLine($"Amount Paid:{payment.AmountPaid,15:0.00}");
            sb.AppendLine($"Change:{(payment.Change),20:0.00}");
        }

        // ================= HELPERS =================
        private static string Line(int width)
            => new string('-', width);

        private static string Center(string text, int width)
        {
            int pad = Math.Max(0, (width - text.Length) / 2);
            return new string(' ', pad) + text;
        }


        private static string Trim(string text, int max)
            => string.IsNullOrEmpty(text)
                ? string.Empty
                : text.Length <= max ? text : text[..max];
    }
}