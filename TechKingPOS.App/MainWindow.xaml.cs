using System.Windows;
using TechKingPOS.App.Services;

namespace TechKingPOS.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            LoggerService.Info(
                "🚀",
                "SYSTEM",
                "Application started"
            );
        }

        private void OpenSales_Click(object sender, RoutedEventArgs e)
        {
            var win = new SalesWindow();
            win.Show();

            LoggerService.Info(
                "🧾💰",
                "SALES",
                "Sales window opened from MainWindow"
            );
        }

        private void OpenAddItem_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddItemWindow();
            win.Show();

            LoggerService.Info(
                "➕📦",
                "INVENTORY",
                "Add Item window opened from MainWindow"
            );
        }
        private void Reports_Click(object sender, RoutedEventArgs e)
{
         new ReportsWindow().ShowDialog();
}
        private void ManageStock_Click(object sender, RoutedEventArgs e)
        {
            var win = new ManageStockWindow();
            win.Show();

            LoggerService.Info(
                "🛠️📦",
                "INVENTORY",
                "Manage Stock window opened from MainWindow"
            );
        }

    }
}
