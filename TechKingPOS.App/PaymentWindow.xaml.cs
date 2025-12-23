using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using TechKingPOS.App.Models;

namespace TechKingPOS.App
{
    public partial class PaymentWindow : Window
    {
        private bool _isLoaded = false;

        public PaymentResult Result { get; private set; }

        private readonly decimal _total;

        public PaymentWindow(decimal total)
        {
            InitializeComponent();

            _total = total;
            Result = new PaymentResult(total);

            TotalText.Text = total.ToString("0.00");

            _isLoaded = true;
        }

        // ----------------------------
        // PAYMENT METHOD CHANGED
        // UI VISIBILITY ONLY
        // ----------------------------
        private void PaymentMethodChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;

            string method = GetSelectedMethod();

            // Default: hide all
            CashPanel.Visibility = Visibility.Collapsed;
            MpesaPanel.Visibility = Visibility.Collapsed;
            CustomerPanel.Visibility = Visibility.Collapsed;

            switch (method)
            {
                case "Cash only":
                    CashPanel.Visibility = Visibility.Visible;
                    break;

                case "Cash + Mpesa":
                    CashPanel.Visibility = Visibility.Visible;
                    MpesaPanel.Visibility = Visibility.Visible;
                    break;

                case "Mpesa only":
                    MpesaPanel.Visibility = Visibility.Visible;
                    break;

                case "Credit":
                    CustomerPanel.Visibility = Visibility.Visible;
                    break;
            }

            UpdateCustomerPanelByAmount();
        }

        // ----------------------------
        // PAYMENT CHANGED
        // AUTO CREDIT VISIBILITY
        // ----------------------------
        private void PaymentChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCustomerPanelByAmount();
        }

        // ----------------------------
        // CONFIRM
        // BUSINESS RULES ONLY
        // ----------------------------
        private void Confirm_Click(object sender, RoutedEventArgs e)
{
    decimal cash = ParseDecimal(CashTextBox.Text);
    decimal mpesa = ParseDecimal(MpesaTextBox.Text);
    decimal paid = cash + mpesa;

    bool isCredit = CustomerPanel.Visibility == Visibility.Visible;

    // ❗ Credit → customer details ONLY
    if (isCredit)
    {
        if (string.IsNullOrWhiteSpace(CustomerNameTextBox.Text) ||
            string.IsNullOrWhiteSpace(PhoneTextBox.Text))
        {
            MessageBox.Show(
                "Customer name and phone are required for credit sales.",
                "Missing Information",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }
    }
    else
    {
        // ❗ Non-credit → must pay something
        if (paid <= 0)
        {
            MessageBox.Show(
                "Enter cash or mpesa amount.",
                "Payment Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }
    }

    // ✅ ACCEPT PAYMENT
    Result.SetPayment(
        cash,
        mpesa,
        CustomerNameTextBox.Text?.Trim(),
        PhoneTextBox.Text?.Trim()
    );

    DialogResult = true;
    Close();
}

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // ----------------------------
        // CUSTOMER PANEL AUTO LOGIC
        // ----------------------------
        private void UpdateCustomerPanelByAmount()
        {
            string method = GetSelectedMethod();

            decimal cash = ParseDecimal(CashTextBox.Text);
            decimal mpesa = ParseDecimal(MpesaTextBox.Text);
            decimal paid = cash + mpesa;

            // Always show for direct credit
            if (method == "Credit")
            {
                CustomerPanel.Visibility = Visibility.Visible;
                return;
            }

            // Auto credit if underpaid
            if (paid > 0 && paid < _total)
            {
                CustomerPanel.Visibility = Visibility.Visible;
            }
            else
            {
                CustomerPanel.Visibility = Visibility.Collapsed;
            }
        }

        // ----------------------------
        // HELPERS
        // ----------------------------
        private string GetSelectedMethod()
        {
            return (PaymentMethodComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()
                   ?? "Cash only";
        }

        private decimal ParseDecimal(string? text)
        {
            if (decimal.TryParse(
                text,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var value))
                return value;

            return 0;
        }
    }
}
