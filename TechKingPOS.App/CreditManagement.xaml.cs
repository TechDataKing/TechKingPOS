using System;
using System.Windows;
using TechKingPOS.App.Data;

namespace TechKingPOS.App
{
    public partial class CreditManagement : Window
    {
        public CreditManagement()
        {
            InitializeComponent();
        }
        private void AddCredit_Click(object sender, RoutedEventArgs e)
{
    ((MainWindow)Application.Current.MainWindow)
        .OpenSalesFromChild();
}

private void FindCustomer_Click(object sender, RoutedEventArgs e)
{
    ((MainWindow)Application.Current.MainWindow)
        .OpenReportsFromChild();
}

private void CreditSummary_Click(object sender, RoutedEventArgs e)
{
    ((MainWindow)Application.Current.MainWindow)
        .OpenReportsFromChild();
}

private void PaymentHistory_Click(object sender, RoutedEventArgs e)
{
    ((MainWindow)Application.Current.MainWindow)
        .OpenReportsFromChild();
}


        private void SubmitPayment_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text) ||
                string.IsNullOrWhiteSpace(AmountBox.Text) ||
                MethodBox.SelectedItem == null)
            {
                MessageBox.Show("Fill all fields");
                return;
            }

            if (!decimal.TryParse(AmountBox.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Invalid amount");
                return;
            }

            var customer = CreditRepository.FindCustomer(SearchBox.Text.Trim());
            if (customer == null)
            {
                MessageBox.Show("Customer not found");
                return;
            }

            var method = ((System.Windows.Controls.ComboBoxItem)
                MethodBox.SelectedItem).Content.ToString();

            CreditRepository.AddPayment(customer.Id, amount, method);

            StatusText.Text = "Payment added successfully";
            AmountBox.Clear();
        }
        private void LoadCustomerBalance(string search)
{
    var customer = CreditRepository.FindCustomer(search);
    if (customer == null)
    {
        BalanceText.Text = "Customer not found";
        BalanceText.Foreground = System.Windows.Media.Brushes.Gray;
        return;
    }

    decimal balance = CreditRepository.GetCustomerBalance(customer.Id);

    BalanceText.Text = $"KES {balance:N2}";
    BalanceText.Foreground = balance > 0
        ? System.Windows.Media.Brushes.DarkRed
        : System.Windows.Media.Brushes.Green;
}
private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
{
    if (string.IsNullOrWhiteSpace(SearchBox.Text))
    {
        BalanceText.Text = "";
        return;
    }

    LoadCustomerBalance(SearchBox.Text.Trim());
}


    }
}
