using System;
using System.Windows;
using TechKingPOS.App.Services;
using TechKingPOS.App.Data;
using TechKingPOS.App.Models;

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
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                StatusText.Text = "Enter username and password";
                return;
            }

            Worker user = WorkerRepository.FindByEmailOrId(username);

            if (user == null || user.IsActive == 0)
            {
                StatusText.Text = "Invalid login details";
                return;
            }

            if (!PasswordService.Verify(password, user.PasswordHash))
            {
                StatusText.Text = "Incorrect password";
                return;
            }

            // ✅ LOGIN SUCCESS
            SessionService.Login(user);

            var main = new MainWindow();
            main.Show();

            Close();
        }
        
    }
}
