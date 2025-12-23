using System.Windows;
using TechKingPOS.App.Data;
using TechKingPOS.App.Models;
using System.Windows.Controls;

namespace TechKingPOS.App
{
    public partial class SettingsWindow : Window
    {
        private AppSetting _settings;
        private enum SettingsSection
        {
            Business,
            Receipt,
            Sales,
            Users,
            System
        }

        private SettingsSection _currentSection;

        public SettingsWindow()
        {
            InitializeComponent();
            
            _settings = SettingsRepository.Get();

            if (_settings == null)
                _settings = new AppSetting();

            EnableDiscountCheck.Checked += (_, _) => UpdateDiscountUI();
            EnableDiscountCheck.Unchecked += (_, _) => UpdateDiscountUI();

            FixedDiscountCheck.Checked += (_, _) => UpdateDiscountUI();
            PercentageDiscountCheck.Checked += (_, _) => UpdateDiscountUI();
            ConditionalDiscountCheck.Checked += (_, _) => UpdateDiscountUI();

            CondValueFixed.Checked += (_, _) => UpdateDiscountUI();
            CondValuePercent.Checked += (_, _) => UpdateDiscountUI();

            CondBasedAbove.Checked += (_, _) => UpdateDiscountUI();
            CondBasedRanges.Checked += (_, _) => UpdateDiscountUI();

            FixedDiscountCheck.Checked += (_, _) =>
            {
                ResetOtherDiscountTypes(FixedDiscountCheck);
                UpdateDiscountUI();
            };

            PercentageDiscountCheck.Checked += (_, _) =>
            {
                ResetOtherDiscountTypes(PercentageDiscountCheck);
                UpdateDiscountUI();
            };

            ConditionalDiscountCheck.Checked += (_, _) =>
            {
                ResetOtherDiscountTypes(ConditionalDiscountCheck);
                UpdateDiscountUI();
            };

            // Initial state
            UpdateDiscountUI();
            LoadSettings();
            ShowBusiness();
        }

        private void LoadSettings()
        {
            _settings = SettingsRepository.Get();

            if (_settings == null)
            {
                _settings = new AppSetting();
                SettingsRepository.Save(_settings);
            }

            // BUSINESS
            BusinessNameBox.Text = _settings.BusinessName ?? "";
            PhoneBox.Text = _settings.Phone ?? "";
            FooterBox.Text = _settings.ReceiptFooter ?? "";

            // DISCOUNTS
            EnableDiscountCheck.IsChecked = _settings.EnableDiscounts;

            FixedDiscountCheck.IsChecked = _settings.AllowFixedDiscount;
            PercentageDiscountCheck.IsChecked = _settings.AllowPercentageDiscount;
            ConditionalDiscountCheck.IsChecked = _settings.AllowConditionalDiscount;

            MaxFixedDiscountBox.Text = _settings.MaxFixedDiscount.ToString();
            MaxPercentageDiscountBox.Text = _settings.MaxPercentageDiscount.ToString();

            ConditionalMinSubtotalBox.Text = _settings.ConditionalMinSubtotal.ToString();
            ConditionalDiscountAmountBox.Text = _settings.ConditionalDiscountAmount.ToString();
        }

        private void SaveSettings()
        {
            _settings.EnableDiscounts = EnableDiscountCheck.IsChecked == true;

            _settings.AllowFixedDiscount = FixedDiscountCheck.IsChecked == true;
            _settings.AllowPercentageDiscount = PercentageDiscountCheck.IsChecked == true;
            _settings.AllowConditionalDiscount = ConditionalDiscountCheck.IsChecked == true;

            decimal.TryParse(MaxFixedDiscountBox.Text, out var maxFixed);
            decimal.TryParse(MaxPercentageDiscountBox.Text, out var maxPercent);
            decimal.TryParse(ConditionalMinSubtotalBox.Text, out var minSubtotal);
            decimal.TryParse(ConditionalDiscountAmountBox.Text, out var conditionalAmount);

            _settings.MaxFixedDiscount = maxFixed;
            _settings.MaxPercentageDiscount = maxPercent;
            _settings.ConditionalMinSubtotal = minSubtotal;
            _settings.ConditionalDiscountAmount = conditionalAmount;

            _settings.BusinessName = BusinessNameBox.Text;
            _settings.Phone = PhoneBox.Text;
            _settings.ReceiptFooter = FooterBox.Text;

            SettingsRepository.Save(_settings);
            MessageBox.Show("Settings saved successfully");
        }

        // ✅ ONLY ADDED METHOD (SAVE BUTTON HANDLER)
        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        // ================= NAVIGATION =================

        private void HideAll()
        {
            BusinessPanel.Visibility = Visibility.Collapsed;
            ReceiptPanel.Visibility = Visibility.Collapsed;
            SalesPanel.Visibility = Visibility.Collapsed;
            UsersPanel.Visibility = Visibility.Collapsed;
            SystemPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowBusiness()
        {
            HideAll();
            BusinessPanel.Visibility = Visibility.Visible;
        }

        private void Business_Click(object sender, RoutedEventArgs e) => ShowBusiness();

        private void Receipt_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            ReceiptPanel.Visibility = Visibility.Visible;
        }

        private void Sales_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            SalesPanel.Visibility = Visibility.Visible;
        }

        private void Users_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            UsersPanel.Visibility = Visibility.Visible;
        }

        private void System_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            SystemPanel.Visibility = Visibility.Visible;
        }

        private void UpdateDiscountUI()
        {
            bool enabled = EnableDiscountCheck.IsChecked == true;

            FixedDiscountCheck.IsEnabled = enabled;
            PercentageDiscountCheck.IsEnabled = enabled;
            ConditionalDiscountCheck.IsEnabled = enabled;

            MaxFixedDiscountBox.Visibility = Visibility.Collapsed;
            MaxPercentageDiscountBox.Visibility = Visibility.Collapsed;

            ConditionalDiscountCheck.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            CondValueFixed.Visibility = Visibility.Collapsed;
            CondValuePercent.Visibility = Visibility.Collapsed;
            CondBasedAbove.Visibility = Visibility.Collapsed;
            CondBasedRanges.Visibility = Visibility.Collapsed;

            AboveSalesPanel.Visibility = Visibility.Collapsed;
            ConditionalDiscountAmountBox.Visibility = Visibility.Collapsed;
            RangeSalesPanel.Visibility = Visibility.Collapsed;

            if (!enabled) return;

            if (FixedDiscountCheck.IsChecked == true)
            {
                MaxFixedDiscountBox.Visibility = Visibility.Visible;
                return;
            }

            if (PercentageDiscountCheck.IsChecked == true)
            {
                MaxPercentageDiscountBox.Visibility = Visibility.Visible;
                return;
            }

            if (ConditionalDiscountCheck.IsChecked == true)
            {
                CondValueFixed.Visibility = Visibility.Visible;
                CondValuePercent.Visibility = Visibility.Visible;

                if (CondValueFixed.IsChecked == true || CondValuePercent.IsChecked == true)
                {
                    CondBasedAbove.Visibility = Visibility.Visible;
                    CondBasedRanges.Visibility = Visibility.Visible;

                    if (CondBasedAbove.IsChecked == true)
                    {
                        AboveSalesPanel.Visibility = Visibility.Visible;
                        ConditionalDiscountAmountBox.Visibility = Visibility.Visible;
                    }
                    else if (CondBasedRanges.IsChecked == true)
                    {
                        RangeSalesPanel.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void ResetOtherDiscountTypes(CheckBox active)
        {
            if (active != FixedDiscountCheck) FixedDiscountCheck.IsChecked = false;
            if (active != PercentageDiscountCheck) PercentageDiscountCheck.IsChecked = false;
            if (active != ConditionalDiscountCheck) ConditionalDiscountCheck.IsChecked = false;
        }
    }
}
