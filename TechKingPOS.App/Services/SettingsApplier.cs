using System;
using System.Windows;
using System.Windows.Controls;
using TechKingPOS.App.Models;
using TechKingPOS.App.Data;

namespace TechKingPOS.App.Services
{
    public static class SettingsApplier
    {
        /// <summary>
        /// Apply the current saved settings to the window's UI and internal variables.
        /// This method automatically updates TextBoxes, CheckBoxes, ComboBoxes, and Buttons
        /// based on control naming convention.
        /// </summary>
        public static void Apply(Window window)
        {
            if (window == null) return;

            var s = SettingsCache.Current;
            if (s == null) return;

            // Iterate all controls in the logical tree
            foreach (var child in LogicalTreeHelper.GetChildren(window))
            {
                if (child is TextBox tb)
                {
                    switch (tb.Name)
                    {
                        case "BusinessNameBox":
                        case "BusinessNameLabel":
                            tb.Text = s.BusinessName;
                            break;
                        case "BranchNameBox":
                        case "BranchNameLabel":
                            tb.Text = s.BranchName;
                            break;
                        case "PhoneBox":
                        case "PhoneLabel":
                            tb.Text = s.Phone;
                            break;
                        case "EmailBox":
                        case "EmailLabel":
                            tb.Text = s.Email;
                            break;
                        case "AddressBox":
                        case "AddressLabel":
                            tb.Text = s.PhysicalAddress;
                            break;
                        case "FooterBox":
                        case "FooterLabel":
                            tb.Text = s.ReceiptFooter;
                            break;
                        case "ReceiptCopiesBox":
                            tb.Text = s.ReceiptCopies.ToString();
                            break;
                    }
                }

                if (child is CheckBox cb)
                {
                    switch (cb.Name)
                    {
                        case "AutoPrintCheck":
                            cb.IsChecked = s.AutoPrintReceipt;
                            break;
                        case "ShowCashierCheck":
                            cb.IsChecked = s.ShowCashierOnReceipt;
                            break;
                        case "ShowLogoCheck":
                            cb.IsChecked = s.ShowLogoOnReceipt;
                            break;
                        case "AllowNegativeStockCheck":
                            cb.IsChecked = s.AllowNegativeStock;
                            break;
                        case "AllowPriceEditCheck":
                            cb.IsChecked = s.AllowPriceEditDuringSale;
                            break;
                        case "EnableCreditSalesCheck":
                            cb.IsChecked = s.EnableCreditSales;
                            break;
                        case "VatEnabledCheck":
                            cb.IsChecked = s.VatEnabled;
                            break;
                        case "RequireLoginCheck":
                            cb.IsChecked = s.RequireLogin;
                            break;
                        case "AutoLogoutCheck":
                            cb.IsChecked = s.AutoLogout;
                            break;
                        case "AllowVoidSalesCheck":
                            cb.IsChecked = s.AllowVoidSales;
                            break;
                        case "AllowWorkersEditPricesCheck":
                            cb.IsChecked = s.AllowWorkersEditPrices;
                            break;
                        case "AllowWorkersGiveDiscountsCheck":
                            cb.IsChecked = s.AllowWorkersGiveDiscounts;
                            break;
                        case "EnableDiscountCheck":
                            cb.IsChecked = s.EnableDiscounts;
                            break;
                        case "AllowFixedDiscountCheck":
                            cb.IsChecked = s.AllowFixedDiscount;
                            break;
                        case "AllowPercentageDiscountCheck":
                            cb.IsChecked = s.AllowPercentageDiscount;
                            break;
                        case "AllowConditionalDiscountCheck":
                            cb.IsChecked = s.AllowConditionalDiscount;
                            break;
                    }
                }

                if (child is ComboBox combo)
                {
                    switch (combo.Name)
                    {
                        case "PaperSizeCombo":
                            combo.SelectedItem = s.PaperSize;
                            break;
                    }
                }

                if (child is Button btn)
                {
                    switch (btn.Name)
                    {
                        case "DiscountButton":
                            btn.IsEnabled = s.EnableDiscounts;
                            break;
                    }
                }

                // Optional: attach the AppSetting to the window for internal use
                window.Tag = s;
            }

            // Update window internal variables if they exist
            dynamic win = window;
            try
            {
                win.VatEnabled = s.VatEnabled;
                win.VatPercent = s.VatPercent;

                win.DiscountEnabled = s.EnableDiscounts;
                win.MaxFixedDiscount = s.MaxFixedDiscount;
                win.MaxPercentageDiscount = s.MaxPercentageDiscount;
                win.CondValueFixed = s.CondValueFixed;
                win.CondValuePercent = s.CondValuePercent;
                win.CondBasedRanges = s.CondBasedRanges;
                win.ConditionalMinSubtotal = s.ConditionalMinSubtotal;
                win.ConditionalDiscountAmount = s.ConditionalDiscountAmount;
                win.DiscountRanges = s.DiscountRanges;
            }
            catch { /* Window may not have these fields, ignore */ }
        }
    }
}
