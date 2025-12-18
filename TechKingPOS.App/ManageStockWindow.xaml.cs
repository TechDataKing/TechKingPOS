using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TechKingPOS.App.Data;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;

namespace TechKingPOS.App
{
    public partial class ManageStockWindow : Window
    {
        private List<ItemLookup> _allItems = new();

        public ManageStockWindow()
        {
            InitializeComponent();

            LoadItems();

            TargetSearchBox.TextChanged += TargetSearchBox_TextChanged;
            EditSearchBox.TextChanged += EditSearchBox_TextChanged;
            EditItemsList.SelectionChanged += EditItemsList_SelectionChanged;
        }

        // ================= LOAD =================
        private void LoadItems()
        {
            _allItems = ItemRepository.GetAllItems();

            // Targets tab → items WITHOUT target
            TargetGrid.ItemsSource = _allItems
                .Where(i => i.TargetQuantity == null)
                .ToList();

            LoggerService.Info("🎯", "STOCK", "Targets loaded",
                $"Count={TargetGrid.Items.Count}");
        }

        // ================= TARGET SEARCH =================
        private void TargetSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = TargetSearchBox.Text.Trim().ToLower();

            TargetGrid.ItemsSource = _allItems
                .Where(i =>
                    i.TargetQuantity == null &&
                    i.Name.ToLower().Contains(text))
                .ToList();
        }

        private void SetTarget_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not ItemLookup item)
                return;

            string input = Microsoft.VisualBasic.Interaction.InputBox(
                $"Set target for {item.Name}",
                "Set Target");

            if (!int.TryParse(input, out int target) || target < 0)
                return;

            ItemRepository.SetTarget(item.Id, target);

            LoggerService.Info("🎯", "STOCK", "Target set",
                $"{item.Name} → {target}");

            LoadItems();
        }

        // ================= EDIT SEARCH =================
        private void EditSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = EditSearchBox.Text.Trim().ToLower();

            EditItemsList.ItemsSource = _allItems
                .Where(i => i.Name.ToLower().Contains(text))
                .ToList();
        }

        private void EditItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EditItemsList.SelectedItem is not ItemLookup item)
                return;

            NameBox.Text = item.Name;
            AliasBox.Text = item.Alias;
            QtyBox.Text = item.Quantity.ToString();
            MPBox.Text = item.MarkedPrice.ToString();
            SPBox.Text = item.SellingPrice.ToString();
            TargetBox.Text = item.TargetQuantity?.ToString() ?? "";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
{
    if (EditItemsList.SelectedItem is not ItemLookup item)
    {
        MessageBox.Show("Select an item first.");
        return;
    }

    if (!int.TryParse(QtyBox.Text, out int qty))
    {
        MessageBox.Show("Invalid quantity");
        return;
    }

    if (!decimal.TryParse(MPBox.Text, out decimal mp))
    {
        MessageBox.Show("Invalid marked price");
        return;
    }

    if (!decimal.TryParse(SPBox.Text, out decimal sp))
    {
        MessageBox.Show("Invalid selling price");
        return;
    }

    int? target = int.TryParse(TargetBox.Text, out int t) ? t : null;

    ItemRepository.UpdateItem(
        item.Id,
        NameBox.Text.Trim(),
        AliasBox.Text.Trim(),
        qty,
        mp,
        sp,
        target
    );

    MessageBox.Show("Item saved");

    ClearEditForm();
    LoadItems();
}


        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (EditItemsList.SelectedItem is not ItemLookup item)
                return;

            if (MessageBox.Show(
                $"Delete {item.Name}?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            ItemRepository.DeleteItem(item.Id);
            LoggerService.Info("🗑️", "STOCK", "Item deleted", item.Name);
            LoadItems();
        }
        private void ClearEditForm()
{
    EditItemsList.SelectedItem = null;

    NameBox.Clear();
    AliasBox.Clear();
    QtyBox.Clear();
    MPBox.Clear();
    SPBox.Clear();
    TargetBox.Clear();
}

    }
}
