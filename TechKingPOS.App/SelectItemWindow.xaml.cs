using System.Windows;
using TechKingPOS.App.Models;
using System.Windows.Controls;


namespace TechKingPOS.App
{
    public partial class SelectItemWindow : Window
    {
        public SaleItem Item { get; private set; }
        public List<RepackRuleModel> RepackRules { get; }


        public SelectItemWindow(SaleItem item, List<RepackRuleModel>? rules = null)
        {
            InitializeComponent();

            Item = item;
            RepackRules = rules ?? new List<RepackRuleModel>();

            DataContext = Item;

            if (RepackRules.Any())
                ShowRepackUI();
        }

        private void ShowRepackUI()
        {
            RepackPanel.Visibility = Visibility.Visible;

            RepackCombo.ItemsSource = RepackRules.Where(r => r.IsActive).ToList();
            RepackCombo.SelectedIndex = 0; // auto-select first rule
        }
private void RepackCombo_Changed(object sender, SelectionChangedEventArgs e)
{
    if (RepackCombo.SelectedItem is RepackRuleModel rule)
    {
        Item.UnitValue = rule.UnitValue;      // 0.25
        Item.UnitType = rule.UnitType;        // kg
        Item.Quantity = 1m;                   // default pack count
        Item.Price = rule.SellingPrice;       // price per pack
        Item.IsRepack = true;
    }
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
/*  */