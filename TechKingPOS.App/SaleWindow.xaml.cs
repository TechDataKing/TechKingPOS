using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TechKingPOS.App.Data;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;
using TechKingPOS.App.Security;

using System.Windows.Media;


namespace TechKingPOS.App
{
    public partial class SalesWindow : RefreshOnFocusWindow

    {
        // ================= DISCOUNT STATE =================
        private bool _isLoaded;
        // Is any discount applied?
        private bool _hasDiscount;
        
        // Final calculated discount amount (KES)
        private decimal _discountAmount;

        // Discount type: "Fixed", "Percentage", "Conditional"
        private string? _discountType;

        // Raw value entered (e.g. 500 or 10)
        private decimal _discountValue;

            private long _lastItemVersion = -1;
            private long _lastSettingsVersion = -1;


        // Cart (right side)
        private ObservableCollection<SaleItem> CartItems = new();

        // All items from DB (left side)
        private List<ItemLookup> _allItems = new();
        private List<Button> _customerButtons;
        private Button _activeCustomer;
        // Holds a cart per customer button
        private Dictionary<Button, ObservableCollection<SaleItem>> _customerCarts = new();

        private string? _quickPaymentMethod; // "Cash", "Mpesa", or null

        private AppSetting _settings;
        private enum DiscountMode
        {
            None,
            Fixed,
            Percentage,
            Conditional
        }
        private decimal VatRate =>
    (_settings?.VatEnabled == true && _settings.VatPercent > 0)
        ? _settings.VatPercent / 100m
        : 0m;


protected override void OnContentRendered(EventArgs e)
{
    base.OnContentRendered(e);
    _isLoaded = true;
}



    private DiscountMode _discountMode = DiscountMode.None;        // overall mode
    private DiscountMode _conditionalType = DiscountMode.None; 
        public SalesWindow()
        {
            InitializeComponent();
            SettingsCache.Load();
            ApplyReceiptSettings();

            LoadDiscountSettings();
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
protected override void Refresh()
{
    if (!_isLoaded)
        return;

    // 🔁 SETTINGS
    if (_lastSettingsVersion != SettingsCache.Version)
    {
        _lastSettingsVersion = SettingsCache.Version;
        LoadDiscountSettings();
        ApplyReceiptSettings();
        UpdateTotals(); // 🔥 cart preserved
    }

    // 🔁 ITEMS
    if (_lastItemVersion != ItemRepository.Version)
    {
        _lastItemVersion = ItemRepository.Version;

        var selected = ItemsList.SelectedItem;

        _allItems = ItemRepository.GetAllItems();
        ItemsList.ItemsSource = _allItems;

        // optional: restore selection
        if (selected is ItemLookup old)
        {
            ItemsList.SelectedItem =
                _allItems.FirstOrDefault(i => i.Id == old.Id);
        }
    }
}

        private void LoadDiscountSettings()
{
    // Load settings including discount ranges from DB
    _settings = SettingsRepository.Get();
    if (_settings == null)
    {
        _settings = new AppSetting();
        //MessageBox.Show("No settings found. Using default.");
    }

    // Ensure ranges are loaded
    var ranges = _settings.DiscountRanges ?? new List<DiscountRange>();

    //Determine main discount mode
    if (_settings.EnableDiscounts)
    {
        if (_settings.AllowFixedDiscount)
            _discountMode = DiscountMode.Fixed;
        else if (_settings.AllowPercentageDiscount)
            _discountMode = DiscountMode.Percentage;
        else if (_settings.AllowConditionalDiscount)
        {
            _discountMode = DiscountMode.Conditional;

            // Determine conditional type from ranges
            if (ranges.Any(r => r.DiscountAmount.HasValue))
                _conditionalType = DiscountMode.Fixed;      // conditional fixed KES
            else if (ranges.Any(r => r.DiscountPercent.HasValue))
                _conditionalType = DiscountMode.Percentage; // conditional %
            else
            {
                _conditionalType = DiscountMode.None;
                MessageBox.Show("Conditional discount enabled but no valid ranges in DB!");
            }
        }
        else
            _discountMode = DiscountMode.None;
    }
    else
    {
        _discountMode = DiscountMode.None;
    }

    // Debug: show what was loaded
//     MessageBox.Show($@"
// EnableDiscounts: {_settings.EnableDiscounts}
// AllowFixedDiscount: {_settings.AllowFixedDiscount}
// AllowPercentageDiscount: {_settings.AllowPercentageDiscount}
// AllowConditionalDiscount: {_settings.AllowConditionalDiscount}
// DiscountRanges Count: {ranges.Count}
// DiscountMode: {_discountMode}
// Conditional Type: {_conditionalType}
// Ranges Details:
// {string.Join("\n", ranges.Select(r => $"From: {r.From}, To: {r.To}, Percent: {r.DiscountPercent}, Amount: {r.DiscountAmount}"))}
// ");
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
                UnitType = dbItem.UnitType,
                Quantity = 1,
                Price = dbItem.SellingPrice,
                MarkedPrice = dbItem.MarkedPrice,
                BranchId = SessionContext.CurrentBranchId
            };

            var rules = RepackRepository
                .GetRulesForItem(dbItem.Id)
                .Where(r => r.IsActive)
                .ToList();

            // ================= STOCK CHECK (INITIAL) =================
            if (!SettingsCache.Current.AllowNegativeStock)
            {
                if (dbItem.Quantity <= 0)
                {
                    MessageBox.Show(
                        $"Out of stock: {dbItem.Name}",
                        "Stock Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }
            }

            var dialog = new SelectItemWindow(saleItem, rules)
            {
                Owner = GetMainWindow()
            };

            if (dialog.ShowDialog() == true)
            {
                // ================= STOCK CHECK (AFTER USER INPUT) =================
                if (!SettingsCache.Current.AllowNegativeStock)
                {
                    if (saleItem.Quantity > dbItem.Quantity)
                    {
                        MessageBox.Show(
                            $"Insufficient stock for {dbItem.Name}\n\n" +
                            $"Available: {dbItem.Quantity}\n" +
                            $"Requested: {saleItem.Quantity}",
                            "Stock Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }
                }

                CartItems.Add(saleItem);
                UpdateTotals();
            }


            ActivityRepository.Log(new Activity
        {
            EntityType = "Item",
            EntityId = saleItem.ItemId,
            EntityName = saleItem.Name,
            Action = "ADD_TO_CART",
            QuantityChange = saleItem.Quantity,
            UnitType = saleItem.UnitType,
            UnitValue = saleItem.Price,
            AfterValue = saleItem.Total.ToString("0.00"),
            PerformedBy = SessionContext.CurrentUserName,
            CreatedAt = DateTime.Now
        });

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
            var beforeQty = SelectedCartItem.Quantity;
            var beforePrice = SelectedCartItem.Price;
            ActivityRepository.Log(new Activity
            {
                EntityType = "Item",
                EntityId = SelectedCartItem.ItemId,
                EntityName = SelectedCartItem.Name,
                Action = "UPDATE_CART_ITEM",
                QuantityChange = SelectedCartItem.Quantity - beforeQty,
                UnitType = SelectedCartItem.UnitType,
                UnitValue = SelectedCartItem.Price,
                BeforeValue = $"{beforeQty} @ {beforePrice}",
                AfterValue = $"{SelectedCartItem.Quantity} @ {SelectedCartItem.Price}",
                PerformedBy = SessionContext.CurrentUserName,
                CreatedAt = DateTime.Now
            });

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
            ActivityRepository.Log(new Activity
            {
                EntityType = "Item",
                EntityId = SelectedCartItem.ItemId,
                EntityName = SelectedCartItem.Name,
                Action = "REMOVE_FROM_CART",
                QuantityChange = -SelectedCartItem.Quantity,
                UnitType = SelectedCartItem.UnitType,
                UnitValue = SelectedCartItem.Price,
                BeforeValue = SelectedCartItem.Total.ToString("0.00"),
                PerformedBy = SessionContext.CurrentUserName,
                CreatedAt = DateTime.Now
            });


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
            ActivityRepository.Log(new Activity
        {
            EntityType = "Sale",
            Action = "CLEAR_CART",
            Reason = "User cleared cart",
            PerformedBy = SessionContext.CurrentUserName,
            CreatedAt = DateTime.Now
        });

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

            decimal discount = CalculateDiscount(subtotal);
            if (discount > subtotal)
                discount = subtotal;

            decimal payable = subtotal - discount;
            if (payable < 0)
                payable = 0;

            decimal vat = 0m;

            if (_settings.VatEnabled)
            {
                vat = Math.Round(payable * VatRate, 2);
            }

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

            // VAT is DISPLAY ONLY

            // 🔥 THIS IS WHAT CUSTOMER PAYS
            TotalAmountText.Text = payable.ToString("0.00");

            UpdateChange();
        }




       private void QuickPayment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;

            string method = btn == CashQuickBtn ? "Cash" : "Mpesa";

            // Toggle OFF
            if (_quickPaymentMethod == method)
            {
                _quickPaymentMethod = null;
                ResetQuickPaymentButtons();

                CashRow.Visibility = Visibility.Collapsed;
                ChangeRow.Visibility = Visibility.Collapsed;
                CashGivenBox.Text = "";
                ChangeText.Text = "0.00";
                return;
            }

            // Switch payment method
            _quickPaymentMethod = method;
            HighlightQuickPaymentButton(btn);

            if (method == "Cash")
            {
                CashRow.Visibility = Visibility.Visible;
                ChangeRow.Visibility = Visibility.Visible;
                CashGivenBox.Focus();
            }
            else
            {
                CashRow.Visibility = Visibility.Collapsed;
                ChangeRow.Visibility = Visibility.Collapsed;
            }
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
            if (CartItems.Count == 0)
            {
                CashRow.Visibility = Visibility.Collapsed;
                ChangeRow.Visibility = Visibility.Collapsed;
                return;
            }
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

            decimal grossTotal = CartItems.Sum(i => i.Total); // VAT-inclusive
            decimal discount = CalculateDiscount(grossTotal);

            // VAT extracted once
            decimal vat = 0m;

            if (_settings.VatEnabled)
            {
                vat = Math.Round(
                    grossTotal * VatRate / (1 + VatRate),
                    2
                );
            }

            // Subtotal AFTER VAT & discount
            decimal subtotal = grossTotal - vat - discount;
            if (subtotal < 0) subtotal = 0;

            // Final total
            decimal total = subtotal + vat;

            string cashier = SessionContext.CurrentUserName;

            var payment = new PaymentResult(
                subtotal,
                discount,
                vat,
                total
            );

            // 🔴 FIX STARTS HERE (cash input was missing)
            if (_quickPaymentMethod == "Cash")
            {
                if (!decimal.TryParse(CashGivenBox.Text, out decimal cashGiven) || cashGiven <= 0)
                {
                    MessageBox.Show("Enter a valid cash amount.");
                    return;
                }

                if (cashGiven < total)
                {
                    MessageBox.Show("Cash given is less than total.");
                    return;
                }

                payment.SetPayment(cashGiven, 0, null, null);
            }
            else // Mpesa
            {
                payment.SetPayment(0, total, null, null);
            }
            // 🔴 FIX ENDS HERE

            try
            {
                string receiptNumber = SalesRepository.SaveSale(
                    CartItems.ToList(),
                    cashier,
                    subtotal,
                    discount,
                    vat,
                    total,
                    payment.AmountPaid
                );
                _allItems = ItemRepository.GetAllItems();
                ItemsList.ItemsSource = _allItems;

                PaymentRepository.SavePayment(
                    receiptNumber,
                    _quickPaymentMethod,
                    payment.AmountPaid
                );
                    ShowReceiptWithSettings(
                        CartItems,
                        payment,
                        receiptNumber,
                        cashier
                    );


                CartItems.Clear();
                UpdateTotals();
                CashRow.Visibility = Visibility.Collapsed;
                ChangeRow.Visibility = Visibility.Collapsed;


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
            // ================= CREDIT MODE CHECK =================
            if (!SettingsCache.Current.EnableCreditSales)
            {
                MessageBox.Show(
                    "Credit sales are disabled in settings.\nPlease use quick payment.",
                    "Payment Restricted",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

                    decimal grossTotalOld = CartItems.Sum(i => i.Total); // VAT-inclusive
                    decimal discountOld = CalculateDiscount(grossTotalOld);

                    // extract VAT once
                    decimal vatOld = 0m;

                    if (_settings.VatEnabled)
                    {
                        vatOld = Math.Round(
                            grossTotalOld * VatRate / (1 + VatRate),
                            2
                        );
                    }

                    // net subtotal (after VAT & discount)
                    decimal subtotalOld = grossTotalOld - vatOld - discountOld;
                    if (subtotalOld < 0) subtotalOld = 0;

                    // final total
                    decimal totalOld = subtotalOld + vatOld;


                    var paymentSeed = new PaymentResult(
                    subtotalOld,
                    discountOld,
                    vatOld,
                    totalOld
                );

                var paymentWindow = new PaymentWindow(paymentSeed)
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

            string cashierOld = SessionContext.CurrentUserName;

            try
            {
                string receiptNumber = SalesRepository.SaveSale(
                    CartItems.ToList(),
                    cashierOld,
                    subtotalOld,
                    discountOld,
                    vatOld,
                    totalOld,
                    paymentOld.AmountPaid
                );
                _allItems = ItemRepository.GetAllItems();
                ItemsList.ItemsSource = _allItems;

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
                    ShowReceiptWithSettings(
                        CartItems,
                        paymentOld,
                        receiptNumber,
                        cashierOld
                    );


                CartItems.Clear();
                UpdateTotals();

                CashRow.Visibility = Visibility.Collapsed;
                ChangeRow.Visibility = Visibility.Collapsed;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Sale failed");
            }
        }
           private void Discount_Click(object sender, RoutedEventArgs e)
        {
            if (!_settings.EnableDiscounts)
            {
                _settings = SettingsRepository.Get(); 

                MessageBox.Show("Discounts are disabled in settings.");
                return;
            }

            if (_discountMode == DiscountMode.None)
            {
                MessageBox.Show("No discount type is enabled in settings.");
                return;
            }

            _hasDiscount = !_hasDiscount;

            if (sender is Button btn)
            {
                if (_hasDiscount)
                {
                    btn.Background = Brushes.SeaGreen;
                    btn.Foreground = Brushes.White;
                }
                else
                {
                    btn.Background = Brushes.White;
                    btn.Foreground = Brushes.Black;
                }
            }

            UpdateTotals();
        }


        private decimal CalculateDiscount(decimal subtotal)
        {
            if (!_settings.EnableDiscounts || !_hasDiscount)
                return 0m;

            switch (_discountMode)
            {
                case DiscountMode.Conditional:
                    if (_settings.AllowConditionalDiscount &&
                        _settings.CondBasedRanges &&
                        _settings.DiscountRanges?.Count > 0)
                    {
                        // Find the first matching range for the subtotal
                        var match = _settings.DiscountRanges
                            .OrderBy(r => r.From)
                            .FirstOrDefault(r => subtotal >= r.From && subtotal <= r.To);

                        if (match != null)
                        {
                            // Conditional percentage
                            if (match.DiscountPercent.HasValue)
                                return Math.Round(subtotal * (match.DiscountPercent.Value / 100m), 2);

                            // Conditional fixed amount
                            if (match.DiscountAmount.HasValue)
                                return Math.Min(match.DiscountAmount.Value, subtotal);
                        }
                    }
                    return 0m;

                case DiscountMode.Fixed:
                    return (_settings.AllowFixedDiscount && _settings.MaxFixedDiscount > 0)
                        ? Math.Min(_settings.MaxFixedDiscount, subtotal)
                        : 0m;

                case DiscountMode.Percentage:
                    return (_settings.AllowPercentageDiscount && _settings.MaxPercentageDiscount > 0)
                        ? Math.Round(subtotal * (_settings.MaxPercentageDiscount / 100m), 2)
                        : 0m;

                default:
                    return 0m;
            }
        }


         private void ApplyReceiptSettings()
    {
        var settings = SettingsCache.Current;

        // Safety: settings should always exist
        if (settings == null)
            return;

        // 🔑 SETTINGS CONTROL SALES CHECKBOX
        PrintReceiptCheckBox.IsEnabled = settings.AutoPrintReceipt;
        PrintReceiptCheckBox.IsChecked = settings.AutoPrintReceipt;
    }
    private void ShowReceiptWithSettings(
    IEnumerable<SaleItem> items,
    PaymentResult payment,
    string receiptNumber,
    string cashier)
{
    var settings = SettingsCache.Current;

    // Master switch
    if (settings?.AutoPrintReceipt != true)
    {
        MessageBox.Show(
            "Receipt printing is disabled in settings.",
            "Printing Disabled",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
    }

    int copies = settings.ReceiptCopies <= 0
        ? 1
        : settings.ReceiptCopies;

    for (int i = 0; i < copies; i++)
    {
        string receiptText = ReceiptBuilder.Build(
            items,
            payment,
            receiptNumber,
            cashier
        );

        var preview = new ReceiptPreviewWindow(receiptText)
        {
            Owner = GetMainWindow()
        };

        preview.ShowDialog();
    }
}


            }
        }