using System;
using System.Windows;
using System.Windows.Input;
using TechKingPOS.App.Services;
using TechKingPOS.App.Data;
using TechKingPOS.App.Models;
using TechKingPOS.App.Security;

namespace TechKingPOS.App
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

       private void Login_Click(object sender, RoutedEventArgs e)
{
    StatusText.Text = "";

    string username = UsernameBox.Text.Trim();
    string password = _passwordVisible
        ? PasswordTextBox.Text
        : PasswordBox.Password;

    if (string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(password))
    {
        StatusText.Text = "Enter username and password";
        return;
    }

    // 🔁 ALWAYS LOAD FRESH FROM DB
Worker user = WorkerRepository.FindByEmailOrId(username);

if (user == null)
{
    StatusText.Text = "Invalid login details";
    return;
}

    if (!PasswordService.Verify(password, user.PasswordHash))
    {
// MessageBox.Show(
//     $"VERIFY DEBUG\n\n" +
//     $"Hash from DB:\n{user.PasswordHash}\n\n" +
//     $"Verify result: {PasswordService.Verify(password, user.PasswordHash)}"
// );

        StatusText.Text = "Invalid login details";
        return;
    }

    if (user.IsActive == 0)
    {
        StatusText.Text = "Account inactive. Contact administrator.";
        return;
    }
    
        PasswordTextBox.Visibility = Visibility.Collapsed;
        PasswordBox.Visibility = Visibility.Visible;
        _passwordVisible = false;


    // 🔐 FORCE PASSWORD CHANGE (ONLY IF STILL TRUE IN DB)
    if (user.MustChangePassword == 1)
    {
        SessionService.Login(user);

        var profile = new EditProfileWindow();
        profile.ShowDialog();

        SessionService.Logout();

        PasswordBox.Clear();
        StatusText.Text = "Password updated. Please log in again.";
        return;
    }

    // ✅ NORMAL LOGIN
    BranchService.Load();
    SessionContext.CurrentBranchId = BranchContext.Id;
    SessionService.Login(user);

    var main = new MainWindow();
    Application.Current.MainWindow = main;
    main.Show();
    Close();
}
private void ForgotPassword_Click(object sender, MouseButtonEventArgs e)
{
    StatusText.Text = "";

    string username = UsernameBox.Text.Trim();

    // 1️⃣ REQUIRE USER IDENTIFIER
    if (string.IsNullOrWhiteSpace(username))
    {
        StatusText.Text = "Enter your Email or ID to reset password.";
        UsernameBox.Focus();
        return;
    }

    // 2️⃣ FIND USER
Worker user = WorkerRepository.FindByEmailOrId(username);

if (user == null)
{
    StatusText.Text = "Invalid login details";
    return;
}
    // 3️⃣ DEACTIVATE ACCOUNT (LOCK IT)
    WorkerRepository.DeactivateWorker(user.Id);

    // 4️⃣ CLEAR PASSWORD BOX
    PasswordBox.Clear();

    // 5️⃣ USER FEEDBACK
    StatusText.Text =
        "Account locked. Contact administrator to reset your password.";
}

private bool _passwordVisible = false;

private void TogglePasswordVisibility(object sender, RoutedEventArgs e)
{
    if (_passwordVisible)
    {
        // Hide password
        PasswordBox.Password = PasswordTextBox.Text;
        PasswordBox.Visibility = Visibility.Visible;
        PasswordTextBox.Visibility = Visibility.Collapsed;
    }
    else
    {
        // Show password
        PasswordTextBox.Text = PasswordBox.Password;
        PasswordTextBox.Visibility = Visibility.Visible;
        PasswordBox.Visibility = Visibility.Collapsed;
    }

    _passwordVisible = !_passwordVisible;
}

        
    }
}
