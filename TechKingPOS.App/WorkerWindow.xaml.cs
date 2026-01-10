using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using TechKingPOS.App.Data;
using TechKingPOS.App.Models;
using TechKingPOS.App.Security;
using TechKingPOS.App.Services; 

namespace TechKingPOS.App
{
    public partial class WorkerWindow : Window
    {
        private List<WorkerView> _allWorkers = new();
        private int? _editingWorkerId = null;
        private WorkerView _editingWorker = null;
private int _selectedWorkerId = 0;

private List<Branch> _branches = new();

        public WorkerWindow()
        {
            InitializeComponent();
            LoadWorkers();
            LoadBranches();
        }
private Window GetMainWindow()
{
    var main = Application.Current.MainWindow;

    if (main == null || !main.IsVisible)
        throw new InvalidOperationException("Main window is not available.");

    return main;
}

        private void LoadWorkers()
        { int branchId = SessionContext.CurrentBranchId;
            _allWorkers = WorkerRepository.GetAll();
            WorkersGrid.ItemsSource = _allWorkers;
        }
private void SaveWorker_Click(object sender, RoutedEventArgs e)
{
    if (string.IsNullOrWhiteSpace(NameBox.Text) ||
        string.IsNullOrWhiteSpace(NationalIdBox.Text) ||
        string.IsNullOrWhiteSpace(PhoneBox.Text))
    {
        MessageBox.Show("Name, ID and Phone are required.");
        return;
    }

    var selectedRoleItem = RoleBox.SelectedItem as ComboBoxItem;
    var roleText = selectedRoleItem?.Tag?.ToString();

    UserRole role = roleText == "Admin"
        ? UserRole.Admin
        : UserRole.Worker;

    // ================= EDIT MODE =================
    if (_editingWorker != null)
{
    var confirm = MessageBox.Show(
        "Do you want to edit this worker?",
        "Confirm Edit",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

    if (confirm != MessageBoxResult.Yes)
        return;

    var changes = new Dictionary<string, object>();

    if (NameBox.Text.Trim() != _editingWorker.Name)
        changes["Name"] = NameBox.Text.Trim();

    if (NationalIdBox.Text.Trim() != _editingWorker.NationalId)
        changes["NationalId"] = NationalIdBox.Text.Trim();

    if (PhoneBox.Text.Trim() != _editingWorker.Phone)
        changes["Phone"] = PhoneBox.Text.Trim();

    if (EmailBox.Text.Trim() != _editingWorker.Email)
        changes["Email"] = EmailBox.Text.Trim();

    var selectedRole =
        ((ComboBoxItem)RoleBox.SelectedItem).Tag.ToString() == "Admin"
        ? UserRole.Admin
        : UserRole.Worker;

    if (selectedRole.ToString() != _editingWorker.Role)
        changes["Role"] = (int)selectedRole;

    int branchId = SessionContext.CurrentBranchId;
    if (branchId != _editingWorker.BranchId)
        changes["BranchId"] = branchId;

    if (changes.Count == 0)
    {
        MessageBox.Show("No changes detected.");
        return;
    }

    WorkerRepository.UpdateProfileByAdmin(
        _editingWorker.Id,
        changes);

    MessageBox.Show("Worker updated successfully.");

    _editingWorker = null;
    _editingWorkerId = null;
    ResetWorkerSelection();
}

    // ================= CREATE MODE =================
    else
    {
        WorkerRepository.Insert(
            NameBox.Text.Trim(),
            NationalIdBox.Text.Trim(),
            PhoneBox.Text.Trim(),
            EmailBox.Text.Trim(),
            role,
            SessionContext.CurrentBranchId
        );

        MessageBox.Show("Worker created successfully.");
    }

    // RESET FORM
    NameBox.Clear();
    NationalIdBox.Clear();
    PhoneBox.Clear();
    EmailBox.Clear();
    RoleBox.SelectedIndex = 1;

    LoadWorkers();
}



        private void Activate_Click(object sender, RoutedEventArgs e)
        {
            if (WorkersGrid.SelectedItem is not WorkerView worker)
                return;

            WorkerRepository.ActivateWorker(worker.Id);
            LoadWorkers();
        }

        private void Deactivate_Click(object sender, RoutedEventArgs e)
        {
            if (WorkersGrid.SelectedItem is not WorkerView worker)
                return;

            WorkerRepository.DeactivateWorker(worker.Id);
            LoadWorkers();
        }

        private void SearchChanged(object sender, TextChangedEventArgs e)
        {
            string text = SearchBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(text))
            {
                WorkersGrid.ItemsSource = _allWorkers;
                return;
            }

            WorkersGrid.ItemsSource = _allWorkers.Where(w =>
                w.Name.ToLower().Contains(text) ||
                w.Phone.ToLower().Contains(text) ||
                w.NationalId.ToLower().Contains(text)
            ).ToList();
        }


private void EditWorker_Click(object sender, RoutedEventArgs e)
{
    if (WorkersGrid.SelectedItem is not WorkerView worker)
    {
        MessageBox.Show("Please select a worker to edit.");
        return;
    }

    if (!UserSession.IsAdmin)
    {
        MessageBox.Show("Only admin can edit worker details.");
        return;
    }

    // ENTER EDIT MODE
    _editingWorker = worker;
    _editingWorkerId = worker.Id;

    // Populate form
    NameBox.Text = worker.Name;
    NationalIdBox.Text = worker.NationalId;
    PhoneBox.Text = worker.Phone;
    EmailBox.Text = worker.Email;

    // Role select
    RoleBox.SelectedIndex =
        worker.Role == "Admin" ? 0 : 1;

    MessageBox.Show("Edit mode enabled. Update details and click Save.");
}
private void AddBranch_Click(object sender, RoutedEventArgs e)
{
    var win = new AddBranchWindow
    {
        Owner = GetMainWindow(),
        WindowStartupLocation = WindowStartupLocation.CenterOwner
    };

    win.ShowDialog();
    LoadBranches();
}
private void EditBranch_Click(object sender, RoutedEventArgs e)
{
    if (BranchCombo.SelectedItem is not Branch branch)
    {
        MessageBox.Show("Select a branch first.");
        return;
    }

    var win = new EditBranchWindow(branch)
    {
        Owner = GetMainWindow(),
        WindowStartupLocation = WindowStartupLocation.CenterOwner
    };

    win.ShowDialog();
    LoadBranches();
}



private void BranchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (BranchCombo.SelectedValue == null)
        return;

    SessionContext.CurrentBranchId =
        (int)BranchCombo.SelectedValue;

    LoadWorkers();
}
private void LoadBranches()
{
    _branches = BranchRepository.GetActive();

    if (_branches.Count == 0)
    {
        MessageBox.Show("No active branches found.");
        return;
    }

    BranchCombo.ItemsSource = _branches;

    // Default → Main / first active
    BranchCombo.SelectedIndex = 0;

    SessionContext.CurrentBranchId =
        (int)BranchCombo.SelectedValue;
}


// <=================== Permissions ===================>

private void Permissions_Click(object sender, RoutedEventArgs e)
{
    if (_selectedWorkerId <= 0)
    {
        MessageBox.Show(
            "Please select a worker first.",
            "No Worker Selected",
            MessageBoxButton.OK,
            MessageBoxImage.Warning
        );
        return;
    }

    PermissionsOverlay.Visibility = Visibility.Visible;

    LoadPermissionsForSelectedWorker();
    HookPermissionCheckboxes();
}
private void LoadPermissionsForSelectedWorker()
{
    foreach (var cb in FindVisualChildren<CheckBox>(PermissionsOverlay))
    {
        if (cb.Tag is not string key)
            continue;

        cb.IsChecked = PermissionRepository.HasPermission(_selectedWorkerId, key);
    }
}
private bool _permissionHooksAttached = false;

private void HookPermissionCheckboxes()
{
    if (_permissionHooksAttached)
        return;

    foreach (var cb in FindVisualChildren<CheckBox>(PermissionsOverlay))
    {
        if (cb.Tag is string)
        {
            cb.Checked += PermissionChanged;
            cb.Unchecked += PermissionChanged;
        }
    }

    _permissionHooksAttached = true;
}

private void PermissionChanged(object sender, RoutedEventArgs e)
{
    if (_selectedWorkerId <= 0)
        return;

    if (sender is not CheckBox cb)
        return;

    if (cb.Tag is not string key)
        return;

    PermissionRepository.SetUserPermission(
        _selectedWorkerId,
        key,
        cb.IsChecked == true
    );
}
private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
    where T : DependencyObject
{
    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
    {
        var child = VisualTreeHelper.GetChild(parent, i);

        if (child is T t)
            yield return t;

        foreach (var descendant in FindVisualChildren<T>(child))
            yield return descendant;
    }
}

private void Window_KeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.Escape && PermissionsOverlay.Visibility == Visibility.Visible)
    {
        PermissionsOverlay.Visibility = Visibility.Collapsed;
        ResetWorkerSelection();
    }
}

private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
{
    PermissionsOverlay.Visibility = Visibility.Collapsed;
    ResetWorkerSelection();
}

private void Card_MouseDown(object sender, MouseButtonEventArgs e)
{
    e.Handled = true; // prevent closing when clicking inside
}

private void ClosePermissions_Click(object sender, RoutedEventArgs e)
{
    PermissionsOverlay.Visibility = Visibility.Collapsed;
    ResetWorkerSelection();
}
private void WorkersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (WorkersGrid.SelectedItem is not WorkerView worker)
    {
        _selectedWorkerId = 0;
        return;
    }

    _selectedWorkerId = worker.Id;
}
private void ResetWorkerSelection()
{
    WorkersGrid.SelectedItem = null;
    _selectedWorkerId = 0;
}

    }
}
