using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TechKingPOS.App.Data;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;

namespace TechKingPOS.App
{
    public partial class SalesWindow : Window
    {
        // Cart (right side)
        private ObservableCollection<SaleItem> CartItems = new();

        // All items from DB (left side)
        private List<ItemLookup> _allItems = new();

        public SalesWindow()
        {
            InitializeComponent();

            CartGrid.ItemsSource = CartItems;

            try
            {
                LoadItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "SalesWindow crash",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }

            SearchBox.TextChanged += SearchBox_TextChanged;
        }

        // ================= LOAD ITEMS =================
        private void LoadItems()
        {
            _allItems = ItemRepository.GetAllItems();

            ItemsList.ItemsSource = _allItems;
            ItemsList.DisplayMemberPath = "Display";

            LoggerService.Info(
                "📦",
                "SALES",
                "Items loaded into sales list",
                $"Count={_allItems.Count}"
            );
        }

        // ================= SEARCH =================
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = SearchBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(text))
            {
                ItemsList.ItemsSource = _allItems;
                return;
            }

            var filtered = _allItems
                .Where(i =>
                    i.Name.ToLower().Contains(text) ||
                    i.Alias.ToLower().Contains(text))
                .ToList();

            ItemsList.ItemsSource = filtered;
        }

        // ================= ADD TO CART =================
        private void ItemsList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ItemsList.SelectedItem is not ItemLookup dbItem)
                return;

            var saleItem = new SaleItem
            {
                ItemId = dbItem.Id,
                Name = dbItem.Name,
                Unit = dbItem.Unit,
                Quantity = 1,
                Price = dbItem.SellingPrice,
                MarkedPrice = dbItem.MarkedPrice
            };

            var dialog = new SelectItemWindow(saleItem)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                CartItems.Add(saleItem);
                UpdateTotals();
            }
        }

        // ================= CART HELPERS =================
        private SaleItem? SelectedCartItem =>
            CartGrid.SelectedItem as SaleItem;

        private void ChangeQty_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCartItem is null)
            {
                MessageBox.Show("Select an item in the cart first.");
                return;
            }

            var dialog = new SelectItemWindow(SelectedCartItem)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                UpdateTotals();
                CartGrid.Items.Refresh();
            }
        }

        private void ChangePrice_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCartItem is null)
            {
                MessageBox.Show("Select an item in the cart first.");
                return;
            }

            var dialog = new SelectItemWindow(SelectedCartItem)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                UpdateTotals();
                CartGrid.Items.Refresh();
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCartItem is null)
            {
                MessageBox.Show("Select an item to delete.");
                return;
            }

            if (MessageBox.Show(
                "Remove selected item from cart?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                CartItems.Remove(SelectedCartItem);
                UpdateTotals();
            }
        }

        private void DeleteAll_Click(object sender, RoutedEventArgs e)
        {
            if (CartItems.Count == 0)
                return;

            if (MessageBox.Show(
                "Clear entire cart?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                CartItems.Clear();
                UpdateTotals();
            }
        }

        // ================= TOTALS =================
        private void UpdateTotals()
        {
            TotalItemsText.Text = CartItems.Sum(i => i.Quantity).ToString();
            TotalAmountText.Text = CartItems.Sum(i => i.Total).ToString("0.00");
        }

        // ================= FINISH & PAY =================
        private void FinishPay_Click(object sender, RoutedEventArgs e)
        {
            if (CartItems.Count == 0)
            {
                MessageBox.Show("Cart is empty.");
                return;
            }

            decimal amountPaid = CartItems.Sum(i => i.Total); // exact for now
            string cashier = "Admin";

            try
            {
                // 💾 SAVE SALE + GET RECEIPT NUMBER
                string receiptNumber = SalesRepository.SaveSale(
                    CartItems.ToList(),
                    cashier,
                    amountPaid
                );

                // 🧾 BUILD RECEIPT
                string receipt = ReceiptBuilder.Build(
                    CartItems,
                    amountPaid,
                    cashier
                );

                // 👀 PREVIEW
                MessageBox.Show(
                    receipt,
                    $"Receipt {receiptNumber}",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // 🧹 CLEAR CART
                CartItems.Clear();
                UpdateTotals();

                LoggerService.Info("🧾", "SALE", "Sale completed", receiptNumber);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Sale failed");
            }
        }
    }
}
