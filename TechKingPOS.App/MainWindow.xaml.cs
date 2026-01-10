using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TechKingPOS.App.Security;
using TechKingPOS.App.Services;


namespace TechKingPOS.App
{
    class WindowStateData
    {
        public bool IsMaximized;
        public double Left;
        public double Top;
        public double Width;
        public double Height;
    }
    public partial class MainWindow : Window
    {   public static MainWindow Instance { get; private set; }
        private Border _draggingWindow;
        private Point _dragOffset;

        private Border _resizingWindow;
        private Point _resizeStart;
        private double _startWidth;
        private double _startHeight;
        private ResizeMode _resizeMode;
        private ResizeDirection _resizeDir;

        private double _startLeft;
        private double _startTop;

        private readonly Dictionary<string, Border> _openWindows = new();
        private readonly Dictionary<string, Button> _taskButtons = new();

        private const double TASKBAR_HEIGHT = 42;
        private const double RESIZE_THICKNESS = 6;

        private enum ResizeMode
        {
            None,
            Right,
            Bottom,
            BottomRight
        }

        [Flags]
        private enum ResizeDirection
        {
            None        = 0,
            Left        = 1,
            Top         = 2,
            Right       = 4,
            Bottom      = 8,

            TopLeft     = Top | Left,
            TopRight    = Top | Right,
            BottomLeft  = Bottom | Left,
            BottomRight = Bottom | Right
        }



        public MainWindow()
        {
            InitializeComponent();
            LoadLoggedInUser();
            
            Instance = this;

             if (SessionService.IsLoggedIn)
            UserNameText.Text = SessionService.CurrentUser.Name;

        }

        // ================= DEFAULT SIZES =================
        private static readonly Dictionary<string, (double Width, double Height)> DefaultWindowSizes
    = new()
{
    { PermissionMap.OpenSales,   (1200, 700) },
    { PermissionMap.OpenAddItem, (500, 660)  },
    { PermissionMap.OpenManageStock, (1200, 700) },
    { PermissionMap.OpenCreditManagement, (900, 650) },
    { PermissionMap.OpenReports, (1200, 700) },
    { PermissionMap.OpenWorkers, (850, 700)  },
    { PermissionMap.OpenSettings,(700, 650)  }
};
private void ApplyDefaultSize(string permissionKey, Border host)
{
    if (!DefaultWindowSizes.TryGetValue(permissionKey, out var size))
        return;

    host.Width = size.Width;
    host.Height = size.Height;
}


        // ================= TASK MENU =================
        private void ToggleTaskMenu(object sender, RoutedEventArgs e)
        {
            TaskMenu.Visibility =
                TaskMenu.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }

        // ================= OPEN WINDOW =================
private void OpenWindow(
    string key,
    Func<Window> factory)
{
   if (!PermissionService.Can(SessionService.CurrentUser.Id, SessionService.CurrentUser.Role, key))
    {
        MessageBox.Show("Access denied", "Permission",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    TaskMenu.Visibility = Visibility.Collapsed;

    if (_openWindows.ContainsKey(key))
    {
        RestoreWindow(key);
        return;
    }

    var win = factory();

    var content = win.Content as UIElement;
    win.Content = null;
    win.Close();

    var host = CreateChildHost(win.Title, content, key);
    ApplyDefaultSize(key, host);

    _openWindows[key] = host;
    Desktop.Children.Add(host);

    Canvas.SetLeft(host, 120);
    Canvas.SetTop(host, 80);

    AddTaskButton(key, win.Title);
    BringToFront(host);
}


        // ================= TASKBAR =================
        private void AddTaskButton(string key, string title)
        {
            var btn = new Button
            {
                Content = title,
                Margin = new Thickness(4, 0, 4, 0),
                Padding = new Thickness(12, 4, 12, 4),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray
            };

            btn.Click += (s, e) => RestoreWindow(key);

            _taskButtons[key] = btn;
            TaskBar.Children.Add(btn);
        }

        private void RestoreWindow(string key)
        {
            var win = _openWindows[key];
            win.Visibility = Visibility.Visible;
            BringToFront(win);
        }

        private void RemoveTaskButton(string key)
        {
            if (_taskButtons.TryGetValue(key, out var btn))
            {
                TaskBar.Children.Remove(btn);
                _taskButtons.Remove(key);
            }
        }

        // ================= CHILD WINDOW =================
        private Border CreateChildHost(string title, UIElement content, string key)
        {
            var container = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Tag = new WindowStateData()
            };

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // title
            root.RowDefinitions.Add(new RowDefinition());                            // content
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // resize row

            root.ColumnDefinitions.Add(new ColumnDefinition());                      // content
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // resize col

            // ===== TITLE BAR =====
            var titleBar = new Grid
            {
                Height = 36,
                Background = Brushes.DarkSlateGray,
                Cursor = Cursors.SizeAll
            };

            titleBar.ColumnDefinitions.Add(new ColumnDefinition());
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = title,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            var buttons = new StackPanel { Orientation = Orientation.Horizontal };

            var minimizeBtn = CreateTitleButton("—");
            var maximizeBtn = CreateTitleButton("▢");
            var closeBtn = CreateTitleButton("✕");

            minimizeBtn.Click += (s, e) => container.Visibility = Visibility.Collapsed;
            maximizeBtn.Click += (s, e) => ToggleMaximize(container);
            closeBtn.Click += (s, e) =>
            {
                Desktop.Children.Remove(container);
                _openWindows.Remove(key);
                RemoveTaskButton(key);
            };

            buttons.Children.Add(minimizeBtn);
            buttons.Children.Add(maximizeBtn);
            buttons.Children.Add(closeBtn);

            Grid.SetColumn(buttons, 1);

            titleBar.Children.Add(titleText);
            titleBar.Children.Add(buttons);

            titleBar.MouseLeftButtonDown += StartDrag;
            titleBar.MouseMove += Drag;
            titleBar.MouseLeftButtonUp += StopDrag;

            root.Children.Add(titleBar);
            Grid.SetRow(titleBar, 0);
            Grid.SetColumnSpan(titleBar, 2);

            // ===== CONTENT =====
            var contentHost = new Border { Child = content };
            root.Children.Add(contentHost);
            Grid.SetRow(contentHost, 1);

            // ===== RESIZE ZONES (FIXED) =====
            var rightResize = new Border
            {
                Width = RESIZE_THICKNESS,
                Cursor = Cursors.SizeWE,
                Background = Brushes.Transparent
            };
            rightResize.MouseLeftButtonDown += (s, e) => StartResize(container, e, ResizeDirection.Right);
            rightResize.MouseMove += Resize;
            rightResize.MouseLeftButtonUp += StopResize;

            var bottomResize = new Border
            {
                Height = RESIZE_THICKNESS,
                Cursor = Cursors.SizeNS,
                Background = Brushes.Transparent
            };
            bottomResize.MouseLeftButtonDown += (s, e) => StartResize(container, e, ResizeDirection.Bottom);
            bottomResize.MouseMove += Resize;
            bottomResize.MouseLeftButtonUp += StopResize;

            var cornerResize = new Border
            {
                Width = RESIZE_THICKNESS,
                Height = RESIZE_THICKNESS,
                Cursor = Cursors.SizeNWSE,
                Background = Brushes.Transparent
            };
            cornerResize.MouseLeftButtonDown += (s, e) => StartResize(container, e, ResizeDirection.BottomRight);
            cornerResize.MouseMove += Resize;
            cornerResize.MouseLeftButtonUp += StopResize;

            root.Children.Add(rightResize);
            Grid.SetRow(rightResize, 1);
            Grid.SetColumn(rightResize, 1);

            root.Children.Add(bottomResize);
            Grid.SetRow(bottomResize, 2);
            Grid.SetColumnSpan(bottomResize, 2);

            root.Children.Add(cornerResize);
            Grid.SetRow(cornerResize, 2);
            Grid.SetColumn(cornerResize, 1);

            container.Child = root;
            return container;
        }

        private Button CreateTitleButton(string text) =>
            new Button
            {
                Content = text,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10),
                Cursor = Cursors.Hand
            };

        // ================= MAXIMIZE =================
        private void ToggleMaximize(Border container)
        {
            var state = (WindowStateData)container.Tag;

            if (!state.IsMaximized)
            {
                state.Left = Canvas.GetLeft(container);
                state.Top = Canvas.GetTop(container);
                state.Width = container.Width;
                state.Height = container.Height;

                Canvas.SetLeft(container, 0);
                Canvas.SetTop(container, TASKBAR_HEIGHT);

                container.Width = Desktop.ActualWidth;
                container.Height = Desktop.ActualHeight - TASKBAR_HEIGHT;

                state.IsMaximized = true;
            }
            else
            {
                Canvas.SetLeft(container, state.Left);
                Canvas.SetTop(container, state.Top);
                container.Width = state.Width;
                container.Height = state.Height;
                state.IsMaximized = false;
            }
        }

        // ================= DRAGGING =================
        private void StartDrag(object sender, MouseButtonEventArgs e)
        {
            var titleBar = (Grid)sender;
            _draggingWindow = (Border)((Grid)titleBar.Parent).Parent;
            _dragOffset = e.GetPosition(_draggingWindow);

            titleBar.CaptureMouse();
            BringToFront(_draggingWindow);
        }

        private void Drag(object sender, MouseEventArgs e)
{
    if (_draggingWindow == null) return;

    var pos = e.GetPosition(Desktop);

    double newLeft = pos.X - _dragOffset.X;
    double newTop  = pos.Y - _dragOffset.Y;

    // ===== HORIZONTAL BOUNDS =====
    if (newLeft < 0)
        newLeft = 0;

    double maxLeft = Desktop.ActualWidth - _draggingWindow.Width;
    if (newLeft > maxLeft)
        newLeft = maxLeft;

    // ===== VERTICAL BOUNDS =====
    if (newTop < TASKBAR_HEIGHT)
        newTop = TASKBAR_HEIGHT;

    double maxTop = Desktop.ActualHeight - _draggingWindow.Height;
    if (newTop > maxTop)
        newTop = maxTop;

    Canvas.SetLeft(_draggingWindow, newLeft);
    Canvas.SetTop(_draggingWindow, newTop);
}


        private void StopDrag(object sender, MouseButtonEventArgs e)
        {
            ((Grid)sender).ReleaseMouseCapture();
            _draggingWindow = null;
        }

        // ================= RESIZING =================
        private void StartResize(Border win, MouseButtonEventArgs e, ResizeDirection dir)
        {
            _resizingWindow = win;
            _resizeDir = dir;
            _resizeStart = e.GetPosition(Desktop);
            _startWidth = win.Width;
            _startHeight = win.Height;
            _startLeft = Canvas.GetLeft(win);
            _startTop = Canvas.GetTop(win);
            ((UIElement)e.Source).CaptureMouse();
        }

        private void Resize(object sender, MouseEventArgs e)
        {
            if (_resizingWindow == null) return;

            var p = e.GetPosition(Desktop);
            var dx = p.X - _resizeStart.X;
            var dy = p.Y - _resizeStart.Y;

            if (_resizeDir.HasFlag(ResizeDirection.Right))
                _resizingWindow.Width = Math.Max(300, _startWidth + dx);

            if (_resizeDir.HasFlag(ResizeDirection.Bottom))
                _resizingWindow.Height = Math.Max(200, _startHeight + dy);

            if (_resizeDir.HasFlag(ResizeDirection.Left))
            {
                _resizingWindow.Width = Math.Max(300, _startWidth - dx);
                Canvas.SetLeft(_resizingWindow, _startLeft + dx);
            }

            if (_resizeDir.HasFlag(ResizeDirection.Top))
            {
                _resizingWindow.Height = Math.Max(200, _startHeight - dy);
                Canvas.SetTop(_resizingWindow, _startTop + dy);
            }
        }

        private void StopResize(object sender, MouseButtonEventArgs e)
        {
            ((UIElement)sender).ReleaseMouseCapture();
            _resizingWindow = null;
        }

        private void BringToFront(Border window)
        {
            foreach (UIElement child in Desktop.Children)
                Panel.SetZIndex(child, 0);

            Panel.SetZIndex(window, 100);
        }

        // ================= ROUTES =================
private void OpenSales(object sender, RoutedEventArgs e) =>
    OpenWindow(PermissionMap.OpenSales, () => new SalesWindow());

private void OpenItems(object sender, RoutedEventArgs e) =>
    OpenWindow(PermissionMap.OpenAddItem, () => new AddItemWindow());

private void OpenStock(object sender, RoutedEventArgs e) =>
    OpenWindow(PermissionMap.OpenManageStock, () => new ManageStockWindow());

private void OpenCredit(object sender, RoutedEventArgs e) =>
    OpenWindow(PermissionMap.OpenCreditManagement, () => new CreditManagement());

private void OpenReports(object sender, RoutedEventArgs e) =>
    OpenWindow(PermissionMap.OpenReports, () =>
    {
        var w = new ReportsWindow();
        w.InitializeReport();
        return w;
    });

private void OpenWorkers(object sender, RoutedEventArgs e) =>
    OpenWindow(PermissionMap.OpenWorkers, () => new WorkerWindow());

private void OpenSettings(object sender, RoutedEventArgs e) =>
    OpenWindow(PermissionMap.OpenSettings, () => new SettingsWindow());


public void OpenReportsFromChild(Action<ReportsWindow> configure)
{
    OpenWindow(PermissionMap.OpenReports, () =>
    {
        var w = new ReportsWindow();
        w.InitializeReport();
        configure?.Invoke(w);
        return w;
    });
}


public void OpenSalesFromChild()
{
    OpenWindow(PermissionMap.OpenSales, () => new SalesWindow());
}




private void LoadLoggedInUser()
{
    if (SessionService.CurrentUser == null)
    {
        UserNameText.Text = "Guest";
        return;
    }

    string fullName = SessionService.CurrentUser.Name ?? "";

    // FIRST NAME ONLY
    string firstName = fullName
        .Trim()
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .FirstOrDefault() ?? "User";

    UserNameText.Text = firstName;
}
        private void ToggleUserMenu(object sender, MouseButtonEventArgs e)
        {
            UserMenu.IsOpen = !UserMenu.IsOpen;
        }

private void Logout_Click(object sender, RoutedEventArgs e)
{
    // End session
    SessionService.Logout();

    // Show login window
    var login = new LoginWindow();
    Application.Current.MainWindow = login;
    login.Show();

    // Close main window
    this.Close();
}

private void EditProfile_Click(object sender, RoutedEventArgs e)
{
    if (!SessionService.IsLoggedIn)
        return;

    var win = new EditProfileWindow
    {
        Owner = this
    };

    win.ShowDialog();

    // Close the dropdown after opening
    UserMenu.IsOpen = false;
}

public static void RefreshUserName(string name)
{
    Instance?.Dispatcher.Invoke(() =>
    {
        Instance.UserNameText.Text = name;
    });
}
private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
{
    // Close task menu if click is outside it
    if (TaskMenu.Visibility == Visibility.Visible &&
        !TaskMenu.IsMouseOver)
    {
        TaskMenu.Visibility = Visibility.Collapsed;
    }

    // Close user dropdown if click is outside it
    if (UserMenu.IsOpen && !UserMenu.IsMouseOver)
    {
        UserMenu.IsOpen = false;
    }
}

private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
{
    if (UserMenu.IsOpen && !UserMenu.IsMouseOver)
    {
        UserMenu.IsOpen = false;
    }
}
private void TaskButton_MouseEnter(object sender, MouseEventArgs e)
{
    TaskButtonHint.IsOpen = true;
}

private void TaskButton_MouseLeave(object sender, MouseEventArgs e)
{
    TaskButtonHint.IsOpen = false;
}

// <! -- PERMISSIONS OVERLAY HANDLERS -- >

   }  
 }
