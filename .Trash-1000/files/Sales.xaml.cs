using System.Windows;
using TechKingPOS.App.Data;
using TechKingPOS.App.Services;

namespace TechKingPOS.App
{
    public partial class SalesWindow : Window
    {
        public SalesWindow()
        {
            InitializeComponent();
            LoadItems();

            LoggerService.Info("🧾", "SALES", "Sales window opened");
        }

        private void LoadItems()
        {
            var items = ItemRepository.GetAllItems();
            ItemsList.ItemsSource = items;
        }
    }
}
