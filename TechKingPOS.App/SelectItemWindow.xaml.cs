using System.Windows;
using TechKingPOS.App.Models;

namespace TechKingPOS.App
{
    public partial class SelectItemWindow : Window
    {
        public SaleItem Item { get; private set; }

        public SelectItemWindow(SaleItem item)
        {
            InitializeComponent();

            Item = item;
            DataContext = Item;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Item.Quantity = 0;
            Item.Price = 0;
            DialogResult = false;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (Item.Quantity <= 0 || Item.Price <= 0)
            {
                MessageBox.Show(
                    "Quantity and selling price must be greater than zero.",
                    "Invalid input",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            DialogResult = true;
        }
    }
}
