using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TechKingPOS.App.Data;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;
using TechKingPOS.App.Security;



namespace TechKingPOS.App
{
    public partial class ManageStockWindow : Window, ISupportBranchRefresh
    {
        private List<ItemLookup> _allItems = new();

        private List<DamagedItemVM> _damagedItems = new();
        // ================= DAMAGED GOODS STATE =================

// all items used for searching (reuse ItemLookup)
private List<ItemLookup> _damagedSearchItems = new();

// currently selected source item (from Items table)
private ItemLookup? _selectedDamagedSourceItem = null;

// in-memory damaged list (until DB table is created)
// ================= REPACK STATE =================
private List<ItemLookup> _repackItems = new();
private ItemLookup? _selectedRepackItem = null;
private RepackRuleModel? _selectedRepackRule = null;
private bool _isEditMode = false;
private TabItem? _lastAllowedTab;
private bool _isLoaded = false;


private bool _isLoading;


        public ManageStockWindow()
        {
            InitializeComponent();

            LoadAllItems();

            TargetSearchBox.TextChanged += TargetSearchBox_TextChanged;
            EditSearchBox.TextChanged += EditSearchBox_TextChanged;
            EditItemsList.SelectionChanged += EditItemsList_SelectionChanged;

             DamageReasonCombo.SelectionChanged += DamageReasonCombo_SelectionChanged;
            DamagedItemSearchList.SelectionChanged += DamagedItemSearchList_SelectionChanged;
            DamagedItemsList.SelectionChanged += DamagedItemsList_SelectionChanged;


        }
        public void RefreshByBranch(int branchId)
{
    // Logic to refresh the window's data based on the new branch ID
    if (branchId != SessionContext.EffectiveBranchId)
    {
        // Update the branch ID in session context (if necessary)
        SessionContext.CurrentBranchId = branchId;

        // Reload the data for this branch (e.g., items, sales records)
        LoadAllItems(); // Re-load items for the new branch
    }
}

private void Window_Loaded(object sender, RoutedEventArgs e)
{
    // Prevent WPF from auto-selecting first tab
    StockTabs.SelectedIndex = -1;

    _isLoading = true;

    // Determine first allowed tab
    TabItem? firstAllowedTab = null;
    foreach (TabItem tab in StockTabs.Items)
    {
        string? key = tab.Tag as string;
        bool allowed = string.IsNullOrWhiteSpace(key) ||
                       PermissionService.Can(SessionService.CurrentUser.Id,
                                             SessionService.CurrentUser.Role,
                                             key);
        tab.IsEnabled = true;
        if (allowed && firstAllowedTab == null)
            firstAllowedTab = tab;
    }

    if (firstAllowedTab != null)
    {
        _lastAllowedTab = firstAllowedTab;
        StockTabs.SelectedItem = firstAllowedTab;
    }

    // Defer heavy loading to ensure SessionContext is ready
    Dispatcher.BeginInvoke(() =>
    {
        LoadAllItems();
        LoadAllItems(); // call legacy method if needed
        _isLoading = false;
    }, DispatcherPriority.Background);
}

/// <summary>
/// Loads items for all tabs in a branch-aware way, similar to SalesWindow.
/// </summary>
private void LoadAllItems()
{
    // ✅ Fetch all items (cumulative or branch-specific)
    _allItems = ItemRepository.GetAllItems();
    

    // -------------------- Targets Tab --------------------
    TargetGrid.ItemsSource = _allItems
        .Where(i => i.TargetQuantity == null) // only items without target
        .ToList();
    TargetGrid.DisplayMemberPath = "Display"; // use computed property
    LoggerService.Info("🎯", "STOCK", "Targets loaded",
                       $"Count={TargetGrid.Items.Count}");

    // -------------------- Edit Tab -----------------------
    EditItemsList.ItemsSource = _allItems.ToList();
    EditItemsList.DisplayMemberPath = "Display";

    // -------------------- Damaged Tab --------------------
    _damagedSearchItems = _allItems.ToList();
    DamagedItemSearchList.ItemsSource = _damagedSearchItems;
    DamagedItemSearchList.DisplayMemberPath = "Display";

    _damagedItems = DamagedRepository.GetAll()
        .Select(d => new DamagedItemVM
        {
            Id = d.Id,
            ItemId = d.ItemId,
            Name = d.ItemName,
            Alias = d.Alias,
            Quantity = d.Quantity,
            SellingPrice = d.SellingPrice,
            Reason = d.Reason,
            CreatedAt = d.DamagedAt
        })
        .ToList();
    DamagedItemsList.ItemsSource = _damagedItems;

    // -------------------- Repack Tab ---------------------
    _repackItems = _allItems.ToList();
    RepackItemList.ItemsSource = _repackItems;
    RepackItemList.DisplayMemberPath = "Display";
}
private void StockTabItem_Loaded(object sender, RoutedEventArgs e)
{
    if (sender is not TabItem tab)
        return;

    string? key = tab.Tag as string;

    bool allowed =
        string.IsNullOrWhiteSpace(key) ||
        PermissionService.Can(
            SessionService.CurrentUser.Id,
            SessionService.CurrentUser.Role,
            key
        );

    if (!allowed)
    {
        // 🚫 PREVENT CONTENT FROM EVER RENDERING
        tab.IsEnabled = false;
        tab.Visibility = Visibility.Collapsed; // optional


        // 🔒 prevent default selection
        if (StockTabs.SelectedItem == tab)
            StockTabs.SelectedIndex = -1;
    }
}

private void StockTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (_isLoading)
        return;

    if (StockTabs.SelectedItem is not TabItem tab)
        return;

    string? permissionKey = tab.Tag as string;

    if (string.IsNullOrWhiteSpace(permissionKey))
    {
        _lastAllowedTab = tab;
        return;
    }

    bool allowed = PermissionService.Can(
        SessionService.CurrentUser.Id,
        SessionService.CurrentUser.Role,
        permissionKey
    );

    if (!allowed)
    {
        Dispatcher.BeginInvoke(() =>
        {
            MessageBox.Show(
                "Access denied",
                "Permission",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );

            _isLoading = true;

            if (_lastAllowedTab != null)
                StockTabs.SelectedItem = _lastAllowedTab;

            _isLoading = false;

        }, DispatcherPriority.Background);

        return;
    }

    _lastAllowedTab = tab;
}

private void StockTabs_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    if (e.OriginalSource is not DependencyObject source)
        return;

    var tab = ItemsControl.ContainerFromElement(
        StockTabs,
        source
    ) as TabItem;

    if (tab == null)
        return;

    string? permissionKey = tab.Tag as string;

    if (string.IsNullOrWhiteSpace(permissionKey))
        return;

    bool allowed = PermissionService.Can(
        SessionService.CurrentUser.Id,
        SessionService.CurrentUser.Role,
        permissionKey
    );

    if (!allowed)
    {
        // 🚫 BLOCK DEFAULT TAB SELECTION
        e.Handled = true;
        Dispatcher.BeginInvoke(() =>
    {
        MessageBox.Show(
            "Access denied",
            "Permission",
            MessageBoxButton.OK,
            MessageBoxImage.Warning
        );
    }, DispatcherPriority.Background);
        
    }
}

        private void LoadRepackItems()
        {
            _repackItems = ItemRepository.GetAllItems();
            RepackItemList.ItemsSource = _repackItems;
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

            LoadAllItems();
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
    LoadAllItems();
    EditItemsList.ItemsSource = _allItems.ToList();
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
            LoadAllItems();
            EditItemsList.ItemsSource = _allItems.ToList();
            ClearEditForm();

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
        private void DamageReasonCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DamageReasonCombo.SelectedItem is ComboBoxItem item &&
                item.Content?.ToString() == "Other")
            {
                OtherReasonBox.Visibility = Visibility.Visible;
                OtherReasonBox.Focus();
            }
            else
            {
                OtherReasonBox.Clear();
                OtherReasonBox.Visibility = Visibility.Collapsed;
            }
        }
        private void ClearDamagedForm_Click(object sender, RoutedEventArgs e)
        {
            ClearDamagedForm();
        }

        private void ClearDamagedForm()
        {
            DamagedItemSearchList.SelectedItem = null;
            _selectedDamagedSourceItem = null;

            DamagedNameBox.Clear();
            DamagedAliasBox.Clear();
            DamagedStockBox.Clear();
            DamagedPriceBox.Clear();
            DamagedQtyBox.Clear();

            DamageReasonCombo.SelectedIndex = -1;
            OtherReasonBox.Clear();
            OtherReasonBox.Visibility = Visibility.Collapsed;
        }


        private void DamagedSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = ((TextBox)sender).Text.Trim().ToLower();

            DamagedItemSearchList.ItemsSource = _damagedSearchItems
                .Where(i =>
                    i.Name.ToLower().Contains(text) ||
                    i.Alias.ToLower().Contains(text))
                .ToList();
        }
        private void DamagedItemSearchList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DamagedItemSearchList.SelectedItem is not ItemLookup item)
                return;

            _selectedDamagedSourceItem = item;

            DamagedNameBox.Text = item.Name;
            DamagedAliasBox.Text = item.Alias;
            DamagedStockBox.Text = item.Quantity.ToString();
            DamagedPriceBox.Text = item.SellingPrice.ToString("0.00");

            DamagedQtyBox.Clear();
        }


        private void AddDamaged_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDamagedSourceItem == null)
            {
                MessageBox.Show("Select an item first");
                return;
            }

            if (!int.TryParse(DamagedQtyBox.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Invalid quantity");
                return;
            }

            if (qty > _selectedDamagedSourceItem.Quantity)
            {
                MessageBox.Show("Quantity exceeds stock");
                return;
            }

            string reason = (DamageReasonCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

            if (reason == "Other")
                reason = OtherReasonBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Provide reason");
                return;
            }

            DamagedRepository.AddDamage(new DamagedItem
            {
                ItemId = _selectedDamagedSourceItem.Id,
                ItemName = _selectedDamagedSourceItem.Name,
                Alias = _selectedDamagedSourceItem.Alias,
                UnitType = _selectedDamagedSourceItem.UnitType,
                Quantity = qty,
                MarkedPrice = _selectedDamagedSourceItem.MarkedPrice,
                SellingPrice = _selectedDamagedSourceItem.SellingPrice,
                Reason = reason,
                RecordedBy = "SYSTEM",
                DamagedAt = DateTime.Now
            });

            LoadDamagedItems();
            LoadAllItems();
            ClearDamagedForm();
        }


    private void DeleteDamaged_Click(object sender, RoutedEventArgs e)
    {
        if (DamagedItemsList.SelectedItem is not DamagedItemVM vm)
            return;

        if (MessageBox.Show(
            $"Delete damaged record for {vm.Name}?",
            "Confirm",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        DamagedRepository.Delete(new DamagedItem
        {
            Id = vm.Id,
            ItemId = vm.ItemId,
            Quantity = vm.Quantity,
            SellingPrice = vm.SellingPrice,
            ItemName = vm.Name
        });

        LoadDamagedItems();
        LoadAllItems();
    }

        private void DamagedItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Needed only to enable Delete button
            // No logic required for now
        }
        private void LoadDamagedItems()
        {
            _damagedItems = DamagedRepository.GetAll()
                .Select(d => new DamagedItemVM
                {   Id = d.Id,
                    ItemId = d.ItemId,
                    Name = d.ItemName,
                    Alias = d.Alias,
                    Quantity = d.Quantity,
                    SellingPrice = d.SellingPrice,
                    Reason = d.Reason,
                    CreatedAt = d.DamagedAt
                })
                .ToList();

            DamagedItemsList.ItemsSource = _damagedItems;
        }
        private void RepackSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = RepackSearchBox.Text.Trim().ToLower();

            RepackItemList.ItemsSource = _repackItems
                .Where(i =>
                    i.Name.ToLower().Contains(text) ||
                    i.Alias.ToLower().Contains(text))
                .ToList();
        }
        private void RepackItemList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RepackItemList.SelectedItem is not ItemLookup item)
                return;

            _selectedRepackItem = item;

            RepackSelectedItemText.Text = $"Item: {item.Name}";
            RepackStockUnitText.Text = item.UnitType;
            RepackAvailableStockText.Text = $"{item.Quantity} {item.UnitType}";

            LoadRepackRules();
        }

        private void RepackRulesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedRepackRule = RepackRulesGrid.SelectedItem as RepackRuleModel;
        }

        private void LoadRepackRules()
        {
            if (_selectedRepackItem == null)
                return;

            var rules = RepackRepository.GetRulesForItem(_selectedRepackItem.Id);

            RepackRulesGrid.ItemsSource = rules;

            // IMPORTANT: reset selection
            _selectedRepackRule = null;
            RepackRulesGrid.SelectedItem = null;
        }

        private void OpenAddRepackRule_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRepackItem == null)
            {
                MessageBox.Show("Select a bulk item first.");
                return;
            }
            _isEditMode = false;

            AddRuleItemText.Text = $"Item: {_selectedRepackItem.Name}";
            AddRuleUnitText.Text = $"Base Unit: {_selectedRepackItem.UnitType}";
            AddRuleUnitSuffix.Text = _selectedRepackItem.UnitType;

            AddRuleQuantityBox.Text = "";
            AddRulePriceBox.Text = "";
            AddRuleActiveCheck.IsChecked = true;

            AddRepackRulePanel.Visibility = Visibility.Visible;


        }
        private void SaveAddRepackRule_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(AddRuleQuantityBox.Text, out var unitValue) || unitValue <= 0)
            {
                MessageBox.Show("Invalid UnitValue.");
                return;
            }

            if (!decimal.TryParse(AddRulePriceBox.Text, out var price) || price <= 0)
            {
                MessageBox.Show("Invalid price.");
                return;
            }

            bool isActive = AddRuleActiveCheck.IsChecked == true;

            // ================= ADD =================
            if (!_isEditMode)
            {
                RepackRepository.AddRule(
                _selectedRepackItem.Id,
                _selectedRepackItem.Name,
                unitValue,
                _selectedRepackItem.UnitType,
                price
            );



                // ✅ ACTIVITY: ADD
                ActivityRepository.Log(new Activity
                {
                    EntityType = "REPACK_RULE",
                    EntityId = _selectedRepackItem.Id, // item context
                    EntityName = _selectedRepackItem.Name,
                    Action = "ADD",
                    QuantityChange = 0,
                    UnitType = _selectedRepackItem.UnitType,
                    UnitValue = unitValue,
                    BeforeValue = null,
                    AfterValue =
                        $"ItemId={_selectedRepackItem.Id}, " +
                        $"UnitValue={unitValue}, Price={price}, Active={isActive}",
                    Reason = "Add repack sale rule",
                    PerformedBy = SessionContext.CurrentUserName,
                    BranchId = SessionContext.EffectiveBranchId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            // ================= EDIT =================
            else
            {
                if (_selectedRepackRule == null)
                    return;

                // SNAPSHOT BEFORE
                var oldUnitValue = _selectedRepackRule.UnitValue;
                var oldPrice = _selectedRepackRule.SellingPrice;
                var oldActive = _selectedRepackRule.IsActive;

                RepackRepository.UpdateRule(
                    _selectedRepackRule.Id,
                    unitValue,
                    price,
                    isActive
                );

                // ✅ ACTIVITY: EDIT
                ActivityRepository.Log(new Activity
                {
                    EntityType = "REPACK_RULE",
                    EntityId = _selectedRepackRule.Id,
                    EntityName = _selectedRepackItem.Name,
                    Action = "EDIT",
                    QuantityChange = 0,
                    UnitType = _selectedRepackRule.UnitType,
                    UnitValue = unitValue,
                    BeforeValue =
                        $"ItemId={_selectedRepackItem.Id}, " +
                        $"UnitValue={oldUnitValue}, Price={oldPrice}, Active={oldActive}",

                    AfterValue =
                        $"ItemId={_selectedRepackItem.Id}, " +
                        $"UnitValue={unitValue}, Price={price}, Active={isActive}",
                    Reason = "Edit repack sale rule",
                    PerformedBy = SessionContext.CurrentUserName,
                    BranchId = SessionContext.EffectiveBranchId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            AddRepackRulePanel.Visibility = Visibility.Collapsed;
            _isEditMode = false;
            LoadRepackRules();
        }


        private void CancelAddRepackRule_Click(object sender, RoutedEventArgs e)
        {
            AddRepackRulePanel.Visibility = Visibility.Collapsed;
        }
        private void DisableRepackRule_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRepackRule == null)
            {
                MessageBox.Show("Select a rule first.");
                return;
            }

            if (MessageBox.Show(
                "Disable this sale rule?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            // SNAPSHOT BEFORE
            var unitValue = _selectedRepackRule.UnitValue;
            var price = _selectedRepackRule.SellingPrice;

            RepackRepository.DisableRule(_selectedRepackRule.Id);

            // ✅ ACTIVITY: DISABLE
            ActivityRepository.Log(new Activity
            {
                EntityType = "REPACK_RULE",
                EntityId = _selectedRepackRule.Id,
                EntityName = _selectedRepackItem.Name,
                Action = "DISABLE",
                QuantityChange = 0,
                UnitType = _selectedRepackRule.UnitType,
                UnitValue = unitValue,
                BeforeValue =
                    $"ItemId={_selectedRepackItem.Id}, " +
                    $"Active=true, UnitValue={unitValue}, Price={price}",

                AfterValue =
                    $"ItemId={_selectedRepackItem.Id}, " +
                    $"Active=false",
                Reason = "Disable repack sale rule",
                PerformedBy = SessionContext.CurrentUserName,
                BranchId = SessionContext.EffectiveBranchId,
                CreatedAt = DateTime.UtcNow
            });

            LoadRepackRules();
        }

        private void DeleteRepackRule_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRepackRule == null)
            {
                MessageBox.Show("Select a rule first.");
                return;
            }

            if (MessageBox.Show(
                "Delete this rule? This action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            // SNAPSHOT BEFORE
            var unitValue = _selectedRepackRule.UnitValue;
            var price = _selectedRepackRule.SellingPrice;

            RepackRepository.DeleteRule(_selectedRepackRule.Id);

            // ✅ ACTIVITY: DELETE
            ActivityRepository.Log(new Activity
            {
                EntityType = "REPACK_RULE",
                EntityId = _selectedRepackRule.Id,
                EntityName = _selectedRepackItem.Name,
                Action = "DELETE",
                QuantityChange = 0,
                UnitType = _selectedRepackRule.UnitType,
                UnitValue = unitValue,
                BeforeValue =
                    $"ItemId={_selectedRepackItem.Id}, " +
                    $"UnitValue={unitValue}, Price={price}",
                AfterValue = null,
                Reason = "Delete repack sale rule",
                PerformedBy = SessionContext.CurrentUserName,
                BranchId = SessionContext.EffectiveBranchId,
                CreatedAt = DateTime.UtcNow
            });

            LoadRepackRules();
        }
        private void EditRepackRule_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRepackRule == null)
            {
                MessageBox.Show("Select a rule first.");
                return;
            }

            _isEditMode = true;

            AddRuleItemText.Text = $"Item: {_selectedRepackRule.ItemName}";
            AddRuleUnitText.Text = $"Base Unit: {_selectedRepackRule.UnitType}";
            AddRuleUnitSuffix.Text = _selectedRepackRule.UnitType;

            AddRuleQuantityBox.Text = _selectedRepackRule.UnitValue.ToString();
            AddRulePriceBox.Text = _selectedRepackRule.SellingPrice.ToString();
            AddRuleActiveCheck.IsChecked = _selectedRepackRule.IsActive;

            AddRepackRulePanel.Visibility = Visibility.Visible;
        }


    }
}
