using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TechKingPOS.App.Data;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;
using System.Windows.Media;

namespace TechKingPOS.App
{
    public partial class SalesWindow : Window

    {
        // ================= DISCOUNT STATE =================

        // Is any discount applied?
        private bool _hasDiscount;

        // Final calculated discount amount (KES)
        private decimal _discountAmount;

        // Discount type: "Fixed", "Percentage", "Conditional"
        private string? _discountType;

        // Raw value entered (e.g. 500 or 10)
        private decimal _discountValue;


        // Cart (right side)
        private ObservableCollection<SaleItem> CartItems = new();

        // All items from DB (left side)
        private List<ItemLookup> _allItems = new();
        private List<Button> _customerButtons;
        private Button _activeCustomer;
        // Holds a cart per customer button
        private Dictionary<Button, ObservableCollection<SaleItem>> _customerCarts = new();

        private string? _quickPaymentMethod; // "Cash", "Mpesa", or null

        private const decimal VAT_RATE = 0.16m;
        private AppSetting _settings;
        public SalesWindow()
        {
            InitializeComponent();

            _settings = SettingsRepository.Get();
                if (_settings == null)
                    _settings = new AppSetting();

            _customerButtons = new List<Button>
     {
                    Customer1Btn,
                    Customer2Btn,
                    Customer3Btn,
                    Customer4Btn,
                    Customer5Btn,
                    Customer6Btn
                };
                 _customerCarts[Customer1Btn] = new ObservableCollection<SaleItem>();

                SetActiveCustomer(Customer1Btn);


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
        private Window GetMainWindow()
        {
            return Application.Current.MainWindow;
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
                Owner = GetMainWindow()
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
                Owner = GetMainWindow() 
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
                Owner = GetMainWindow()
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

        private void AddCustomer_Click(object sender, RoutedEventArgs e)
            {
                var nextHidden = _customerButtons
                    .FirstOrDefault(b => b.Visibility == Visibility.Collapsed);

                if (nextHidden == null)
                {
                    MessageBox.Show("Maximum customers reached");
                    return;
                }

                nextHidden.Visibility = Visibility.Visible;
                SetActiveCustomer(nextHidden);
            }
            private void Customer_Click(object sender, RoutedEventArgs e)
            {
                        if (sender is Button btn)
                        {
                            SetActiveCustomer(btn);
                        }
                    }


            private void SetActiveCustomer(Button btn)
            {
                foreach (var b in _customerButtons)
                {
                    b.Background = Brushes.White;
                    b.Foreground = Brushes.Black;
                }

                btn.Background = Brushes.DodgerBlue;
                btn.Foreground = Brushes.White;

                _activeCustomer = btn;

                // 🔴 MISSING PART — CART SWITCH
                if (!_customerCarts.ContainsKey(btn))
                {
                    _customerCarts[btn] = new ObservableCollection<SaleItem>();
                }

                CartItems = _customerCarts[btn];
                CartGrid.ItemsSource = CartItems;

                UpdateTotals();
            }





        // ================= TOTALS =================

            private void UpdateTotals()
            {
                decimal subtotal = CartItems.Sum(i => i.Total);

                    // 🔥 SETTINGS-DRIVEN DISCOUNT
                decimal discount = CalculateDiscount(subtotal);

                decimal taxableAmount = subtotal - discount;

                if (taxableAmount < 0) taxableAmount = 0;

                decimal tax = Math.Round(taxableAmount * VAT_RATE, 2);
                decimal total = taxableAmount + tax;

                TotalItemsText.Text = CartItems.Sum(i => i.Quantity).ToString();
                SubtotalText.Text = subtotal.ToString("0.00");

                if (discount > 0)
                {
                    DiscountRow.Visibility = Visibility.Visible;
                    DiscountText.Text = "-" + discount.ToString("0.00");
                }
                else
                {
                    DiscountRow.Visibility = Visibility.Collapsed;
                }

                //VatText.Text = tax.ToString("0.00");
                TotalAmountText.Text = total.ToString("0.00");

                UpdateChange();
            }



        private void QuickPayment_Click(object sender, RoutedEventArgs e)
{
    if (sender is not Button btn)
        return;

    string method = btn == CashQuickBtn ? "Cash" : "Mpesa";

    // Toggle OFF if same button clicked again
    if (_quickPaymentMethod == method)
    {
        _quickPaymentMethod = null;
        ResetQuickPaymentButtons();
        return;
    }

    // Switch to new method
    _quickPaymentMethod = method;
    HighlightQuickPaymentButton(btn);
}

        private void HighlightQuickPaymentButton(Button active)
        {
            ResetQuickPaymentButtons();

            active.Background = Brushes.DodgerBlue;
            active.Foreground = Brushes.White;
        }

        private void ResetQuickPaymentButtons()
        {
            CashQuickBtn.Background = Brushes.White;
            CashQuickBtn.Foreground = Brushes.Black;

            MpesaQuickBtn.Background = Brushes.White;
            MpesaQuickBtn.Foreground = Brushes.Black;
        }


        private void CashGiven_TextChanged(object sender, TextChangedEventArgs e)
{
    UpdateChange();
}

private void UpdateChange()
{
    if (CashRow.Visibility != Visibility.Visible)
        return;

    decimal total = decimal.Parse(TotalAmountText.Text);
    decimal cashGiven = 0;

    decimal.TryParse(CashGivenBox.Text, out cashGiven);

    decimal change = cashGiven - total;
    if (change < 0) change = 0;

    ChangeText.Text = change.ToString("0.00");
}




        // ================= FINISH & PAY =================
       private void FinishPay_Click(object sender, RoutedEventArgs e)
{
    // ================= QUICK FULL PAYMENT HANDLER =================
    if (_quickPaymentMethod != null)
    {
        if (CartItems.Count == 0)
        {
            MessageBox.Show("Cart is empty.");
            return;
        }

            decimal subtotal = CartItems.Sum(i => i.Total);
            decimal discount = CalculateDiscount(subtotal);

            decimal taxableAmount = subtotal - discount;
            if (taxableAmount < 0) taxableAmount = 0;

            decimal tax = Math.Round(taxableAmount * VAT_RATE, 2);
            decimal total = taxableAmount + tax;
                    string cashier = "Admin";

        var payment = new PaymentResult(total);

        if (_quickPaymentMethod == "Cash")
            payment.SetPayment(total, 0, null, null);
        else
            payment.SetPayment(0, total, null, null);

        try
        {
            string receiptNumber = SalesRepository.SaveSale(
                CartItems.ToList(),
                cashier,
                payment.AmountPaid
            );

            PaymentRepository.SavePayment(
                receiptNumber,
                _quickPaymentMethod,
                total
            );

            string receiptText = ReceiptBuilder.Build(
                CartItems,
                payment,
                receiptNumber,
                cashier
            );

            var previewWindow = new ReceiptPreviewWindow(receiptText)
            {
                Owner = GetMainWindow()
            };
            previewWindow.ShowDialog();

            CartItems.Clear();
            UpdateTotals();

            _quickPaymentMethod = null;
            ResetQuickPaymentButtons();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Sale failed");
        }

        return; // ⛔ EXIT so old system is NOT triggered
    }

    // ==============================================================
    // ⬇⬇⬇ NOTHING SELECTED → FALL BACK TO OLD SYSTEM ⬇⬇⬇
    // ==============================================================
    
    // 🔴 YOUR ORIGINAL CODE — UNCHANGED 🔴

    if (CartItems.Count == 0)
    {
        MessageBox.Show("Cart is empty.");
        return;
    }

    decimal totalOld = CartItems.Sum(i => i.Total);

    var paymentWindow = new PaymentWindow(totalOld)
    {
        Owner = GetMainWindow()
    };

    if (paymentWindow.ShowDialog() != true)
        return;

    PaymentResult paymentOld = paymentWindow.Result;

    if (paymentOld.AmountPaid <= 0 && paymentOld.Balance <= 0)
    {
        MessageBox.Show("No payment entered.");
        return;
    }

    string cashierOld = "Admin";

    try
    {
        string receiptNumber = SalesRepository.SaveSale(
            CartItems.ToList(),
            cashierOld,
            paymentOld.AmountPaid
        );

        if (paymentOld.CashAmount > 0)
        {
            PaymentRepository.SavePayment(
                receiptNumber,
                "Cash",
                paymentOld.CashAmount
            );
        }

        if (paymentOld.MpesaAmount > 0)
        {
            PaymentRepository.SavePayment(
                receiptNumber,
                "Mpesa",
                paymentOld.MpesaAmount
            );
        }

        if (paymentOld.Balance > 0)
        {
            CreditRepository.SaveCredit(
                receiptNumber,
                paymentOld.CustomerName!,
                paymentOld.Phone,
                paymentOld.Total,
                paymentOld.AmountPaid,
                paymentOld.Balance
            );
        }

        string receiptText = ReceiptBuilder.Build(
            CartItems,
            paymentOld,
            receiptNumber,
            cashierOld
        );

        var previewWindow = new ReceiptPreviewWindow(receiptText)
        {
            Owner = GetMainWindow()
        };
        previewWindow.ShowDialog();

        CartItems.Clear();
        UpdateTotals();
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message, "Sale failed");
    }
}

            private decimal CalculateDiscount(decimal subtotal)
            {
                if (!_settings.EnableDiscounts)
                    return 0m;

                // 1️⃣ CONDITIONAL DISCOUNT (AUTOMATIC)
                if (_settings.AllowConditionalDiscount &&
                    subtotal >= _settings.ConditionalMinSubtotal &&
                    _settings.ConditionalDiscountAmount > 0)
                {
                    return Math.Min(_settings.ConditionalDiscountAmount, subtotal);
                }

                // 2️⃣ FIXED DISCOUNT (MANUAL)
                if (_settings.AllowFixedDiscount && _hasDiscount && _discountAmount > 0)
                {
                    return Math.Min(_discountAmount, _settings.MaxFixedDiscount);
                }

                // 3️⃣ PERCENTAGE DISCOUNT (MANUAL)
                if (_settings.AllowPercentageDiscount && _hasDiscount && _discountValue > 0)
                {
                    decimal percentDiscount = subtotal * (_discountValue / 100m);
                    decimal maxAllowed = subtotal * (_settings.MaxPercentageDiscount / 100m);
                    return Math.Min(percentDiscount, maxAllowed);
                }

                return 0m;
            }


       

            }
        }