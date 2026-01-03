
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using System.Printing;
using System.Windows.Media;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.IO;


using TechKingPOS.App.Models;
using TechKingPOS.App.Data;
using TechKingPOS.App.Services;
using TechKingPOS.App.Security;


namespace TechKingPOS.App
{
    public partial class ReportsWindow : Window
    {
        private bool _isLoaded = false;
         private List<CreditView> _allCredits = new();

        public ReportsWindow()
        {
         InitializeComponent();
         

            // DO NOT call load methods here
        }

        private int _editingExpenseId = 0;
        private int _selectedBranchId = 0; // 0 = All Branches

private enum ReportType
{
    Sales,
    Inventory,
    Credit,
    Expenses
}

private ReportType GetActiveReport()
{
    return (ReportType)ReportsTabControl.SelectedIndex;
}


public void InitializeReport()
{
    _isLoaded = true;

    FromDatePicker.SelectedDate = DateTime.Today;
    ToDatePicker.SelectedDate = DateTime.Today;

    LoadReport();       // TODAY SALES
    LoadOutOfStock();   // INVENTORY DEFAULT
    LoadCredits();      // CREDIT
    LoadExpenses();    // EXPENSES
    LoadBranches();
    

}

        private Window GetMainWindow()
        {
            return Application.Current.MainWindow;
        }
private void ExportPdf_Click(object sender, RoutedEventArgs e)
{
    switch (GetActiveReport())
    {
        case ReportType.Sales:
            ExportSalesPdf();
            break;

        case ReportType.Inventory:
            ExportInventoryPdf();
            break;

        case ReportType.Credit:
            ExportCreditPdf();
            break;

        case ReportType.Expenses:
            ExportExpensesPdf();
            break;
    }
}


private void ExportExcel_Click(object sender, RoutedEventArgs e)
{
    switch (GetActiveReport())
    {
        case ReportType.Sales:
            ExportSalesExcel();
            break;

        case ReportType.Inventory:
            ExportInventoryExcel();
            break;

        case ReportType.Credit:
            ExportCreditExcel();
            break;

        case ReportType.Expenses:
            ExportExpensesExcel();
            break;
    }
}


private void PrintReport_Click(object sender, RoutedEventArgs e)
{
    if (ReportsTabControl.SelectedItem is not TabItem tab)
        return;

    switch (tab.Header.ToString())
    {
        

        case "  Sales Report  ":
            PrintSales();
            break;

        case "  Inventory  ":
            PrintInventory();   
            break;

        case "  Credit  ":
            PrintCredits();
            break;

        case "  Expenses  ":
            PrintExpenses();
            break;
    }
}



        // ================= WINDOW LOADED =================
private void ReportsWindow_Loaded(object sender, RoutedEventArgs e)
{   
    MessageBox.Show("Window Loaded");

    if (BranchComboBox == null)
    {
        MessageBox.Show("BranchComboBox is NULL");
        return;
    }

    MessageBox.Show("BranchComboBox exists");


    FromDatePicker.SelectedDate = DateTime.Today;
    ToDatePicker.SelectedDate = DateTime.Today;

    Dispatcher.BeginInvoke(
        new Action(LoadReport),
        System.Windows.Threading.DispatcherPriority.Background
    );

    LoadOutOfStock();
    LoadCredits();
    LoadBranches();

}




        // ================= SALES =================

        private void LoadToday()
        {
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;
        }

        private void LoadReport_Click(object sender, RoutedEventArgs e)
        {
            LoadReport();
        }

        private void LoadReport()
        {
            if (FromDatePicker.SelectedDate == null ||
                ToDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Select date range.");
                return;
            }

            DateTime from = FromDatePicker.SelectedDate.Value.Date;
            DateTime to = ToDatePicker.SelectedDate.Value
                .Date
                .AddDays(1)
                .AddSeconds(-1);

            var summary = ReportsRepository.GetSalesSummary(from, to, _selectedBranchId);

            TotalSalesText.Text = summary.TotalSales.ToString("0.00");
            ProfitText.Text = summary.Profit.ToString("0.00");
            ReceiptCountText.Text = summary.ReceiptCount.ToString();

            SoldItemsGrid.ItemsSource =
                ReportsRepository.GetSoldItems(from, to, _selectedBranchId);
        }

        // ================= INVENTORY =================

        private void InventoryFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            // 🔒 CRITICAL GUARD
            if (!_isLoaded)
                return;

            if (InventoryFilterCombo.SelectedItem is not ComboBoxItem item)
                return;

            string filter = item.Content.ToString();

            switch (filter)
            {
                case "Out of Stock":
                    LoadOutOfStock();
                    break;

                case "Running Low":
                    LoadRunningLow();
                    break;

                case "Good Stock":
                    LoadGoodStock();
                    break;
            }
        }

        private void LoadOutOfStock()
        {
            InventoryGrid.ItemsSource =
                InventoryRepository.GetOutOfStock();
        }

        private void LoadRunningLow()
        {
            InventoryGrid.ItemsSource =
                InventoryRepository.GetRunningLow();
        }

        private void LoadGoodStock()
        {
            InventoryGrid.ItemsSource =
                InventoryRepository.GetGoodStock();
        }
        // ================= CREDITS =================  
private void LoadCredits(DateTime? from= null,DateTime? to=null)
{
    _allCredits.Clear();
    decimal totalCredit = 0;

    using var conn = DbService.GetConnection();
    conn.Open();

    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        SELECT
            c.Id,
            cust.Name,
            cust.Phone,
            c.Total,
            c.Paid,
            c.Balance,
            COALESCE(
                (
                    SELECT MAX(cp.CreatedAt)
                    FROM CreditPayments cp
                    WHERE cp.CreditId = c.Id
                ),
                c.CreatedAt
            ) AS LastPaymentDate
        FROM Credits c
        JOIN Customers cust ON cust.Id = c.CustomerId
        WHERE c.Balance > 0
          AND (@from IS NULL OR c.CreatedAt >= @from)
          AND (@to IS NULL OR c.CreatedAt <= @to)
        ORDER BY LastPaymentDate DESC;
    ";

    cmd.Parameters.AddWithValue("@from",
        from.HasValue ? from.Value.ToString("yyyy-MM-dd") : DBNull.Value);

    cmd.Parameters.AddWithValue("@to",
        to.HasValue ? to.Value.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);

    using var reader = cmd.ExecuteReader();

    while (reader.Read())
    {
        decimal balance = reader.GetDecimal(5);
        DateTime lastPayment = DateTime.Parse(reader.GetString(6));
        totalCredit += balance;

        _allCredits.Add(new CreditView
        {
            CreditId = reader.GetInt32(0),
            CustomerName = reader.GetString(1),
            Phone = reader.IsDBNull(2) ? "" : reader.GetString(2),
            Total = reader.GetDecimal(3),
            Paid = reader.GetDecimal(4),
            Balance = balance,
            LastPaymentDate = lastPayment
        });
    }

    CreditGrid.ItemsSource = _allCredits;
    TotalCreditText.Text = totalCredit.ToString("0.00");
}


// ================= CREDIT SEARCH =================
private void CreditSearchChanged(object sender, TextChangedEventArgs e)
{
    string text = CreditSearchTextBox.Text.Trim().ToLower();

    if (string.IsNullOrEmpty(text))
    {
        CreditGrid.ItemsSource = _allCredits;
        return;
    }

    CreditGrid.ItemsSource = _allCredits.Where(c =>
        c.CustomerName.ToLower().Contains(text) ||
        c.Phone.ToLower().Contains(text)
    ).ToList();
}
private void LoadTodayExpenses()
{
    LoadExpenses();
}
private void LoadExpenses()
{
    var from = DateTime.Today;
    var to = DateTime.Today;

    var list = ExpenseRepository.GetExpenses(from, to);

    ExpensesGrid.ItemsSource = list;
    TotalExpensesText.Text =
        ExpenseRepository.GetTotalExpenses(from, to).ToString("0.00");
}


private void SaveExpense_Click(object sender, RoutedEventArgs e)
{
    // ===== VALIDATION =====
    if (ExpenseDatePicker.SelectedDate == null ||
        ExpenseCategoryCombo.SelectedItem == null ||
        ExpensePaymentCombo.SelectedItem == null ||
        string.IsNullOrWhiteSpace(ExpenseDescriptionTextBox.Text) ||
        string.IsNullOrWhiteSpace(ExpenseAmountTextBox.Text))
    {
        MessageBox.Show(
            "Please fill in all fields before saving the expense.",
            "Missing Information",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
    }

    if (!decimal.TryParse(ExpenseAmountTextBox.Text, out var amount) || amount <= 0)
    {
        MessageBox.Show(
            "Please enter a valid expense amount.",
            "Invalid Amount",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
    }

    var category =
        ((ComboBoxItem)ExpenseCategoryCombo.SelectedItem).Content.ToString();

    var payment =
        ((ComboBoxItem)ExpensePaymentCombo.SelectedItem).Content.ToString();

    // ===== SAVE / UPDATE =====
    if (_editingExpenseId == 0)
    {
        ExpenseRepository.AddExpense(
            ExpenseDatePicker.SelectedDate.Value,
            category,
            ExpenseDescriptionTextBox.Text.Trim(),
            amount,
            payment);

        MessageBox.Show(
            "Expense added successfully.",
            "Success",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
    else
    {
        ExpenseRepository.UpdateExpense(
            _editingExpenseId,
            ExpenseDatePicker.SelectedDate.Value,
            category,
            ExpenseDescriptionTextBox.Text.Trim(),
            amount,
            payment);

        MessageBox.Show(
            "Expense updated successfully.",
            "Success",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    ClearExpenseForm();
    LoadExpenses();
}

private void EditExpense_Click(object sender, RoutedEventArgs e)
{
    if (ExpensesGrid.SelectedItem is not Expense exp)
        return;

    _editingExpenseId = exp.Id;

    ExpenseFormTitle.Text = "✏ Edit Expense";
    SaveExpenseButton.Content = "Update Expense";

    ExpenseDatePicker.SelectedDate = exp.Date;
    ExpenseDescriptionTextBox.Text = exp.Description;
    ExpenseAmountTextBox.Text = exp.Amount.ToString("0.00");

    SelectComboItem(ExpenseCategoryCombo, exp.Category);
    SelectComboItem(ExpensePaymentCombo, exp.PaymentMethod);
}
private void SelectComboItem(ComboBox combo, string value)
{
    foreach (ComboBoxItem item in combo.Items)
        if (item.Content.ToString() == value)
            combo.SelectedItem = item;
}
private void DeleteExpense_Click(object sender, RoutedEventArgs e)
{
    if (ExpensesGrid.SelectedItem is not Expense exp)
        return;

    var result = MessageBox.Show(
        "Are you sure you want to delete this expense?",
        "Confirm Delete",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);

    if (result != MessageBoxResult.Yes)
        return;

    ExpenseRepository.DeleteExpense(exp.Id);

    MessageBox.Show(
        "Expense deleted successfully.",
        "Deleted",
        MessageBoxButton.OK,
        MessageBoxImage.Information);

    LoadExpenses();
}

private void ClearExpenseForm()
{
    _editingExpenseId = 0;

    ExpenseFormTitle.Text = "➕ Add Expense";
    SaveExpenseButton.Content = "Save Expense";

    ExpenseDatePicker.SelectedDate = DateTime.Today;
    ExpenseCategoryCombo.SelectedIndex = -1;
    ExpensePaymentCombo.SelectedIndex = -1;
    ExpenseDescriptionTextBox.Clear();
    ExpenseAmountTextBox.Clear();
}

private void PrintCredits()
{
    if (_allCredits.Count == 0)
    {
        MessageBox.Show("No credit records to print.");
        return;
    }

    var doc = new FlowDocument
    {
        PagePadding = new Thickness(40),
        FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
        FontSize = 12
    };

    // ===== HEADER =====
    doc.Blocks.Add(new Paragraph(new Run("CREDIT ACCOUNTS REPORT"))
    {
        FontSize = 18,
        FontWeight = FontWeights.Bold,
        TextAlignment = TextAlignment.Center
    });

    doc.Blocks.Add(new Paragraph(
        new Run($"Printed on: {DateTime.Now:yyyy-MM-dd HH:mm}")));

    doc.Blocks.Add(new Paragraph(
        new Run($"Total Outstanding Credit: {TotalCreditText.Text}"))
    {
        FontWeight = FontWeights.SemiBold
    });
    // ===== DATE RANGE =====
string range =
    CreditFromDatePicker.SelectedDate != null
        ? $"Period: {CreditFromDatePicker.SelectedDate:yyyy-MM-dd} to {CreditToDatePicker.SelectedDate:yyyy-MM-dd}"
        : "Period: ALL";

doc.Blocks.Add(new Paragraph(new Run(range))
{
    FontStyle = FontStyles.Italic,
    Margin = new Thickness(0, 0, 0, 10)
});


    // ===== TABLE =====
    var table = new Table();
    doc.Blocks.Add(table);

    table.Columns.Add(new TableColumn { Width = new GridLength(50) });   // No.
    table.Columns.Add(new TableColumn { Width = new GridLength(200) });  // Customer
    table.Columns.Add(new TableColumn { Width = new GridLength(140) });  // Phone
    table.Columns.Add(new TableColumn { Width = new GridLength(120) });  // Balance
    table.Columns.Add(new TableColumn { Width = new GridLength(120) });  // Last Payment

    var header = new TableRowGroup();
    table.RowGroups.Add(header);

    header.Rows.Add(new TableRow
    {
        FontWeight = FontWeights.Bold,
        Cells =
        {
            new TableCell(new Paragraph(new Run("No."))),
            new TableCell(new Paragraph(new Run("Customer"))),
            new TableCell(new Paragraph(new Run("Phone"))),
            new TableCell(new Paragraph(new Run("Balance"))),
            new TableCell(new Paragraph(new Run("Last Payment")))
        }
    });


    var body = new TableRowGroup();
    table.RowGroups.Add(body);

        int index = 1;

        foreach (var c in _allCredits)
        {
            body.Rows.Add(new TableRow
            {
                Cells =
                {
                    new TableCell(new Paragraph(new Run(index.ToString()))),
                    new TableCell(new Paragraph(new Run(c.CustomerName))),
                    new TableCell(new Paragraph(new Run(c.Phone))),
                    new TableCell(new Paragraph(new Run(c.Balance.ToString("0.00")))),
                    new TableCell(new Paragraph(new Run(c.LastPaymentDate.ToString("yyyy-MM-dd"))))
                }
            });

            index++;
        }


    // ===== PRINT =====
        var preview = new PrintPreviewWindow(doc)
        {
            Owner = GetMainWindow()
        };

        preview.ShowDialog();


}
private void ApplyCreditDateFilter_Click(object sender, RoutedEventArgs e)
{
    DateTime? from = CreditFromDatePicker.SelectedDate;
    DateTime? to = CreditToDatePicker.SelectedDate;

    if (from != null && to != null && from > to)
    {
        MessageBox.Show("From date cannot be later than To date.");
        return;
    }

    LoadCredits(from, to);
}
private void ClearCreditDateFilter_Click(object sender, RoutedEventArgs e)
{
    CreditFromDatePicker.SelectedDate = null;
    CreditToDatePicker.SelectedDate = null;

    LoadCredits(); // reload ALL outstanding credits
}

private void PrintSales_Click(object sender, RoutedEventArgs e)
{
    PrintSales();
}

private void PrintInventory_Click(object sender, RoutedEventArgs e)
{
    PrintInventory();
}

private void PrintExpenses_Click(object sender, RoutedEventArgs e)
{
    PrintExpenses();
}
private void PrintSales()
{
    if (SoldItemsGrid.Items.Count == 0)
    {
        MessageBox.Show("No sales records to print.");
        return;
    }

    var doc = new FlowDocument
    {
        PagePadding = new Thickness(40),
        FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
        FontSize = 12
    };

    // ===== TITLE =====
    doc.Blocks.Add(new Paragraph(new Run("SALES REPORT"))
    {
        FontSize = 18,
        FontWeight = FontWeights.Bold,
        TextAlignment = TextAlignment.Center
    });

    // ===== DATE RANGE =====
    string range =
        FromDatePicker.SelectedDate != null
            ? $"Period: {FromDatePicker.SelectedDate:yyyy-MM-dd} to {ToDatePicker.SelectedDate:yyyy-MM-dd}"
            : "Period: ALL";

    doc.Blocks.Add(new Paragraph(new Run(range))
    {
        FontStyle = FontStyles.Italic,
        Margin = new Thickness(0, 5, 0, 10)
    });

    // ===== SUMMARY =====
    doc.Blocks.Add(new Paragraph(new Run($"Total Sales: {TotalSalesText.Text}")));
    doc.Blocks.Add(new Paragraph(new Run($"Profit: {ProfitText.Text}")));
    doc.Blocks.Add(new Paragraph(new Run($"Receipts: {ReceiptCountText.Text}"))
    {
        Margin = new Thickness(0, 0, 0, 15)
    });

    // ===== TABLE =====
    var table = new Table();
    doc.Blocks.Add(table);

    table.Columns.Add(new TableColumn { Width = new GridLength(35) });    // No
    table.Columns.Add(new TableColumn { Width = new GridLength(150) });   // Receipt
    table.Columns.Add(new TableColumn { Width = new GridLength(180) });   // Item
    table.Columns.Add(new TableColumn { Width = new GridLength(60) });    // Qty
    table.Columns.Add(new TableColumn { Width = new GridLength(80) });    // Price
    table.Columns.Add(new TableColumn { Width = new GridLength(90) });    // Total

    var header = new TableRowGroup();
    table.RowGroups.Add(header);

    header.Rows.Add(new TableRow
    {
        FontWeight = FontWeights.Bold,
        Cells =
        {
            new TableCell(new Paragraph(new Run("#"))),
            new TableCell(new Paragraph(new Run("Receipt"))),
            new TableCell(new Paragraph(new Run("Item"))),
            new TableCell(new Paragraph(new Run("Qty"))),
            new TableCell(new Paragraph(new Run("Price"))),
            new TableCell(new Paragraph(new Run("Total")))
        }
    });

    var body = new TableRowGroup();
    table.RowGroups.Add(body);

    int index = 1;

    foreach (var item in SoldItemsGrid.Items.Cast<SoldItemReport>())
    {
        body.Rows.Add(new TableRow
        {
            Cells =
            {
                new TableCell(new Paragraph(new Run(index.ToString()))),
                new TableCell(new Paragraph(new Run(item.ReceiptNumber))),
                new TableCell(new Paragraph(new Run(item.ItemName))),
                new TableCell(new Paragraph(new Run(item.Quantity.ToString()))),
                new TableCell(new Paragraph(new Run(item.Price.ToString("0.00")))),
                new TableCell(new Paragraph(new Run(item.Total.ToString("0.00"))))
            }
        });

        index++;
    }

    // ===== PRINT PREVIEW =====
    var preview = new PrintPreviewWindow(doc)
    {
        Owner = GetMainWindow()
    };

    preview.ShowDialog();
}

private void LoadInventoryByDate_Click(object sender, RoutedEventArgs e)
{
    DateTime? from = InventoryFromDatePicker.SelectedDate;
    DateTime? to = InventoryToDatePicker.SelectedDate;

    if (from == null || to == null)
    {
        MessageBox.Show("Please select both From and To dates.");
        return;
    }

    // TODO: Replace with your repository call
    // Example:
    // InventoryGrid.ItemsSource = InventoryRepository.GetByDate(from.Value, to.Value);

    MessageBox.Show($"Inventory filtered from {from:yyyy-MM-dd} to {to:yyyy-MM-dd}");
}
private void LoadExpensesByDate_Click(object sender, RoutedEventArgs e)
{
    DateTime? from = ExpenseFromDatePicker.SelectedDate;
    DateTime? to = ExpenseToDatePicker.SelectedDate;

    if (from == null || to == null)
    {
        MessageBox.Show("Please select both From and To dates.");
        return;
    }

    // TODO: Replace with your repository call
    // Example:
    // ExpensesGrid.ItemsSource = ExpenseRepository.GetByDate(from.Value, to.Value);

    MessageBox.Show($"Expenses filtered from {from:yyyy-MM-dd} to {to:yyyy-MM-dd}");
        }
        private TableCell H(string text)
        {
            return new TableCell(new Paragraph(new Run(text)))
            {
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(6),
                BorderThickness = new Thickness(0.5),
                BorderBrush = Brushes.Black
            };
        }

        private TableCell C(string text)
        {
            return new TableCell(new Paragraph(new Run(text)))
            {
                Padding = new Thickness(6),
                BorderThickness = new Thickness(0.5),
                BorderBrush = Brushes.Black
            };
        }
private void PrintInventory()
{
    var items = InventoryGrid.ItemsSource as IEnumerable<ItemLookup>;
    if (items == null || !items.Any())
    {
        MessageBox.Show("No inventory data to print.");
        return;
    }

    var doc = new FlowDocument
    {
        PagePadding = new Thickness(40),
        FontFamily = new FontFamily("Segoe UI"),
        FontSize = 12
    };

    doc.Blocks.Add(new Paragraph(new Run("INVENTORY REPORT"))
    {
        FontSize = 18,
        FontWeight = FontWeights.Bold,
        TextAlignment = TextAlignment.Center
    });

    var table = new Table();
    doc.Blocks.Add(table);

    table.Columns.Add(new TableColumn { Width = new GridLength(40) });
    table.Columns.Add(new TableColumn { Width = new GridLength(220) });
    table.Columns.Add(new TableColumn { Width = new GridLength(80) });
    table.Columns.Add(new TableColumn { Width = new GridLength(100) });
    table.Columns.Add(new TableColumn { Width = new GridLength(100) });

    var header = new TableRowGroup();
    table.RowGroups.Add(header);

    var headerRow = new TableRow();
    headerRow.Cells.Add(H("#"));
    headerRow.Cells.Add(H("Item"));
    headerRow.Cells.Add(H("Qty"));
    headerRow.Cells.Add(H("Target"));
    headerRow.Cells.Add(H("Deficit"));
    header.Rows.Add(headerRow);

    var body = new TableRowGroup();
    table.RowGroups.Add(body);

    int index = 1;
    foreach (var i in items)
    {
        var row = new TableRow();
        row.Cells.Add(C(index++.ToString()));
        row.Cells.Add(C(i.Name));
        row.Cells.Add(C(i.Quantity.ToString()));
        row.Cells.Add(C(i.TargetQuantity?.ToString() ?? "-"));
        row.Cells.Add(C(i.Deficit.ToString()));
        body.Rows.Add(row);
    }

    new PrintPreviewWindow(doc)
    {
        Owner = GetMainWindow()
    }.ShowDialog();
}
private void PrintExpenses()
{
    var expenses = ExpensesGrid.ItemsSource as IEnumerable<Expense>;
    if (expenses == null || !expenses.Any())
    {
        MessageBox.Show("No expenses data to print.");
        return;
    }

    var doc = new FlowDocument
    {
        PagePadding = new Thickness(40),
        FontFamily = new FontFamily("Segoe UI"),
        FontSize = 12
    };

    doc.Blocks.Add(new Paragraph(new Run("EXPENSES REPORT"))
    {
        FontSize = 18,
        FontWeight = FontWeights.Bold,
        TextAlignment = TextAlignment.Center
    });

    var table = new Table();
    doc.Blocks.Add(table);

    table.Columns.Add(new TableColumn { Width = new GridLength(40) });
    table.Columns.Add(new TableColumn { Width = new GridLength(100) });
    table.Columns.Add(new TableColumn { Width = new GridLength(140) });
    table.Columns.Add(new TableColumn { Width = new GridLength(240) });
    table.Columns.Add(new TableColumn { Width = new GridLength(100) });

    var header = new TableRowGroup();
    table.RowGroups.Add(header);

    var headerRow = new TableRow();
    headerRow.Cells.Add(H("#"));
    headerRow.Cells.Add(H("Date"));
    headerRow.Cells.Add(H("Category"));
    headerRow.Cells.Add(H("Description"));
    headerRow.Cells.Add(H("Amount"));
    header.Rows.Add(headerRow);

    var body = new TableRowGroup();
    table.RowGroups.Add(body);

    int index = 1;
    foreach (var e in expenses)
    {
        var row = new TableRow();
        row.Cells.Add(C(index++.ToString()));
        row.Cells.Add(C(e.Date.ToString("yyyy-MM-dd")));
        row.Cells.Add(C(e.Category));
        row.Cells.Add(C(e.Description));
        row.Cells.Add(C(e.Amount.ToString("0.00")));
        body.Rows.Add(row);
    }

    new PrintPreviewWindow(doc)
    {
        Owner = GetMainWindow()
    }.ShowDialog();
}
private TableCell Cell(string text, bool bold = false)
{
    return new TableCell(new Paragraph(new Run(text))
    {
        FontWeight = bold ? FontWeights.Bold : FontWeights.Normal
    })
    {
        Padding = new Thickness(4)
    };
}

private FlowDocument CreateBaseDocument(string title)
{
    var doc = new FlowDocument
    {
        FontFamily = new FontFamily("Segoe UI"),
        FontSize = 12
    };

    doc.Blocks.Add(new Paragraph(new Run(title))
    {
        FontSize = 18,
        FontWeight = FontWeights.Bold,
        TextAlignment = TextAlignment.Center
    });

    doc.Blocks.Add(new Paragraph(new Run($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}"))
    {
        FontStyle = FontStyles.Italic,
        TextAlignment = TextAlignment.Center
    });

    doc.Blocks.Add(new Paragraph(new Run(" ")));
    return doc;
}
private void ExportInventoryPdf()
{
    string[] headers = new string[]
    {
        "Item", "Quantity", "Target", "Deficit"
    };

    var rows = new List<string[]>();

    foreach (var item in InventoryGrid.Items)
    {
        if (item == null) continue;
        dynamic r = item;

        rows.Add(new string[]
        {
            r.Name?.ToString() ?? "",
            r.Quantity?.ToString() ?? "",
            r.TargetQuantity?.ToString() ?? "",
            r.Deficit?.ToString() ?? ""
        });
    }

    PdfExporter.Export(
        "Inventory Report",
        "Current Inventory Status",
        headers,
        rows
    );
}
private void ExportCreditPdf()
{
    string[] headers = new string[]
    {
        "Receipt", "Customer", "Phone", "Balance", "Date"
    };

    var rows = new List<string[]>();

    foreach (var item in CreditGrid.Items)
    {
        if (item == null) continue;
        dynamic r = item;

        rows.Add(new string[]
        {
            r.ReceiptNumber?.ToString() ?? "",
            r.CustomerName?.ToString() ?? "",
            r.Phone?.ToString() ?? "",
            r.Balance?.ToString() ?? "0.00",
            r.CreatedAt?.ToString("yyyy-MM-dd") ?? ""
        });
    }

    PdfExporter.Export(
        "Credit Report",
        "Outstanding Credit Accounts",
        headers,
        rows
    );
}
private void ExportExpensesPdf()
{
    string[] headers = new string[]
    {
        "Date", "Category", "Description", "Amount"
    };

    var rows = new List<string[]>();

    foreach (var item in ExpensesGrid.Items)
    {
        if (item == null) continue;
        dynamic r = item;

        rows.Add(new string[]
        {
            r.Date?.ToString("yyyy-MM-dd") ?? "",
            r.Category?.ToString() ?? "",
            r.Description?.ToString() ?? "",
            r.Amount?.ToString("0.00") ?? "0.00"
        });
    }

    PdfExporter.Export(
        "Expenses Report",
        "Expense Summary",
        headers,
        rows
    );
}

private void ExportSalesPdf()
{
    var headers = new[]
    {
        "Receipt", "Item", "Qty", "Price", "Total"
    };

    var rows = new List<string[]>();

    foreach (var item in SoldItemsGrid.Items)
    {
        if (item is SoldItemReport r)
        {
            rows.Add(new[]
            {
                r.ReceiptNumber,
                r.ItemName,
                r.Quantity.ToString(),
                r.Price.ToString("0.00"),
                r.Total.ToString("0.00")
            });
        }
    }

    var period =
        FromDatePicker.SelectedDate != null
        ? $"Period: {FromDatePicker.SelectedDate:yyyy-MM-dd} to {ToDatePicker.SelectedDate:yyyy-MM-dd}"
        : "Period: ALL";

    PdfExporter.Export(
        "Sales Report",
        period,
        headers,
        rows);
}
private void ExportSalesExcel()
{
    var items = SoldItemsGrid.ItemsSource as IEnumerable<SoldItemReport>;
    if (items == null || !items.Any())
    {
        MessageBox.Show("No sales data to export.");
        return;
    }

    var wb = new XLWorkbook();
    var ws = wb.Worksheets.Add("Sales");

    int row = 1;

    // ===== TITLE =====
    ws.Cell(row, 1).Value = "SALES REPORT";
    ws.Range(row, 1, row, 6).Merge().Style
        .Font.SetBold()
        .Font.SetFontSize(16)
        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    row += 2;

    // ===== DATE RANGE =====
    string range =
        FromDatePicker.SelectedDate != null
            ? $"Period: {FromDatePicker.SelectedDate:yyyy-MM-dd} to {ToDatePicker.SelectedDate:yyyy-MM-dd}"
            : "Period: ALL";

    ws.Cell(row, 1).Value = range;
    ws.Range(row, 1, row, 6).Merge();
    row += 2;

    // ===== SUMMARY =====
    ws.Cell(row, 1).Value = "Total Sales:";
    ws.Cell(row, 2).Value = TotalSalesText.Text;
    row++;

    ws.Cell(row, 1).Value = "Profit:";
    ws.Cell(row, 2).Value = ProfitText.Text;
    row++;

    ws.Cell(row, 1).Value = "Receipts:";
    ws.Cell(row, 2).Value = ReceiptCountText.Text;
    row += 2;

    // ===== HEADER =====
    ws.Cell(row, 1).Value = "#";
    ws.Cell(row, 2).Value = "Receipt";
    ws.Cell(row, 3).Value = "Item";
    ws.Cell(row, 4).Value = "Qty";
    ws.Cell(row, 5).Value = "Price";
    ws.Cell(row, 6).Value = "Total";

    ws.Range(row, 1, row, 6).Style.Font.SetBold();
    row++;

    int index = 1;
    foreach (var s in items)
    {
        ws.Cell(row, 1).Value = index++;
        ws.Cell(row, 2).Value = s.ReceiptNumber;
        ws.Cell(row, 3).Value = s.ItemName;
        ws.Cell(row, 4).Value = s.Quantity;
        ws.Cell(row, 5).Value = s.Price;
        ws.Cell(row, 6).Value = s.Total;
        row++;
    }

    ws.Columns().AdjustToContents();

    // ===== SAVE =====
    var dlg = new SaveFileDialog
    {
        Filter = "Excel File (*.xlsx)|*.xlsx",
        FileName = $"Sales_Report_{DateTime.Now:yyyyMMdd}.xlsx"
    };

    if (dlg.ShowDialog() == true)
        wb.SaveAs(dlg.FileName);
}

private void ExportInventoryExcel()
{
    var items = InventoryGrid.ItemsSource as IEnumerable<ItemLookup>;
    if (items == null || !items.Any())
    {
        MessageBox.Show("No inventory data to export.");
        return;
    }

    var dialog = new SaveFileDialog
    {
        Filter = "Excel File (*.xlsx)|*.xlsx",
        FileName = $"Inventory_Report_{DateTime.Now:yyyyMMdd}.xlsx"
    };

    if (dialog.ShowDialog() != true)
        return;

    using var wb = new XLWorkbook();
    var ws = wb.Worksheets.Add("Inventory");

    ws.Cell(1, 1).Value = "#";
    ws.Cell(1, 2).Value = "Item";
    ws.Cell(1, 3).Value = "Quantity";
    ws.Cell(1, 4).Value = "Target";
    ws.Cell(1, 5).Value = "Deficit";

    int row = 2;
    int index = 1;

    foreach (var item in items)
    {
        ws.Cell(row, 1).Value = index++;
        ws.Cell(row, 2).Value = item.Name;
        ws.Cell(row, 3).Value = item.Quantity;
        ws.Cell(row, 4).Value = item.TargetQuantity ?? 0;
        ws.Cell(row, 5).Value = item.Deficit;
        row++;
    }

    ws.Columns().AdjustToContents();
    wb.SaveAs(dialog.FileName);

    MessageBox.Show("Inventory exported successfully.");
}
private void ExportCreditExcel()
{
    if (_allCredits.Count == 0)
    {
        MessageBox.Show("No credit data to export.");
        return;
    }

    var dialog = new SaveFileDialog
    {
        Filter = "Excel File (*.xlsx)|*.xlsx",
        FileName = $"Credit_Report_{DateTime.Now:yyyyMMdd}.xlsx"
    };

    if (dialog.ShowDialog() != true)
        return;

    using var wb = new XLWorkbook();
    var ws = wb.Worksheets.Add("Credit");

    ws.Cell(1, 1).Value = "#";
    ws.Cell(1, 2).Value = "Customer";
    ws.Cell(1, 3).Value = "Phone";
    ws.Cell(1, 4).Value = "Total";
    ws.Cell(1, 5).Value = "Paid";
    ws.Cell(1, 6).Value = "Balance";
    ws.Cell(1, 7).Value = "Last Payment";

    int row = 2;
    int index = 1;

    foreach (var c in _allCredits)
    {
        ws.Cell(row, 1).Value = index++;
        ws.Cell(row, 2).Value = c.CustomerName;
        ws.Cell(row, 3).Value = c.Phone;
        ws.Cell(row, 4).Value = c.Total;
        ws.Cell(row, 5).Value = c.Paid;
        ws.Cell(row, 6).Value = c.Balance;
        ws.Cell(row, 7).Value = c.LastPaymentDate.ToString("yyyy-MM-dd");
        row++;
    }

    ws.Columns().AdjustToContents();
    wb.SaveAs(dialog.FileName);

    MessageBox.Show("Credit report exported successfully.");
}
private void ExportExpensesExcel()
{
    var expenses = ExpensesGrid.ItemsSource as IEnumerable<Expense>;
    if (expenses == null || !expenses.Any())
    {
        MessageBox.Show("No expenses data to export.");
        return;
    }

    var dialog = new SaveFileDialog
    {
        Filter = "Excel File (*.xlsx)|*.xlsx",
        FileName = $"Expenses_Report_{DateTime.Now:yyyyMMdd}.xlsx"
    };

    if (dialog.ShowDialog() != true)
        return;

    using var wb = new XLWorkbook();
    var ws = wb.Worksheets.Add("Expenses");

    ws.Cell(1, 1).Value = "#";
    ws.Cell(1, 2).Value = "Date";
    ws.Cell(1, 3).Value = "Category";
    ws.Cell(1, 4).Value = "Description";
    ws.Cell(1, 5).Value = "Amount";

    int row = 2;
    int index = 1;

    foreach (var e in expenses)
    {
        ws.Cell(row, 1).Value = index++;
        ws.Cell(row, 2).Value = e.Date.ToString("yyyy-MM-dd");
        ws.Cell(row, 3).Value = e.Category;
        ws.Cell(row, 4).Value = e.Description;
        ws.Cell(row, 5).Value = e.Amount;
        row++;
    }

    ws.Columns().AdjustToContents();
    wb.SaveAs(dialog.FileName);

    MessageBox.Show("Expenses exported successfully.");
}
private List<Branch> _branches = new();


private void BranchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (!_isLoaded)
        return;

    if (BranchComboBox.SelectedValue == null)
        return;

    SessionContext.CurrentBranchId =
        (int)BranchComboBox.SelectedValue;

    LoadReport();
    LoadCredits();
    LoadOutOfStock();
}

private void LoadBranches()
{
    _isLoaded = false;

    _branches = BranchRepository.GetActive().ToList();

    if (_branches.Count == 0)
    {
        MessageBox.Show("No active branches found.");
        return;
    }

    // 👉 Add GLOBAL option
    _branches.Insert(0, new Branch
    {
        Id = 0,
        Name = "All Branches"
    });

    BranchComboBox.ItemsSource = _branches;
    BranchComboBox.SelectedIndex = 0;

    SessionContext.CurrentBranchId = 0;

    _isLoaded = true;
}
    }
}