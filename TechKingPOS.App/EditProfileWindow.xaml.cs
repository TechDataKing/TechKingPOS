using System.Windows;
using TechKingPOS.App.Data;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;

namespace TechKingPOS.App
{
    public partial class EditProfileWindow : Window
    {
        private Worker _currentUser;

        public EditProfileWindow()
        {
            InitializeComponent();
            LoadUser();
        }

        // ================= LOAD USER =================
        private void LoadUser()
        {
            _currentUser = SessionService.CurrentUser;

            if (_currentUser == null)
            {
                MessageBox.Show("User session expired.");
                Close();
                return;
            }

            NameBox.Text = _currentUser.Name;
            UsernameBox.Text = _currentUser.Email ?? _currentUser.NationalId;
            EmailBox.Text = _currentUser.Email;
            PhoneBox.Text = _currentUser.Phone;
        }

        // ================= SAVE =================
private void Save_Click(object sender, RoutedEventArgs e)
{
    if (_currentUser == null)
    {
        MessageBox.Show("User session expired.");
        Close();
        return;
    }

    if (string.IsNullOrWhiteSpace(NameBox.Text))
    {
        MessageBox.Show("Name is required.");
        return;
    }

    bool passwordChanged = false;

    // ================= PASSWORD CHANGE =================
    if (!string.IsNullOrWhiteSpace(NewPasswordBox.Password))
    {
        if (string.IsNullOrWhiteSpace(CurrentPasswordBox.Password))
        {
            MessageBox.Show("Enter current password.");
            return;
        }

        if (!PasswordService.Verify(
                CurrentPasswordBox.Password,
                _currentUser.PasswordHash))
        {
            MessageBox.Show("Current password is incorrect.");
            return;
        }

        if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
        {
            MessageBox.Show("Passwords do not match.");
            return;
        }

        string newHash = PasswordService.Hash(NewPasswordBox.Password);

        // ✅ ONLY THESE TWO
        WorkerRepository.ChangePassword(_currentUser.Id, newHash);
        WorkerRepository.SetMustChangePassword(_currentUser.Id, false);

        _currentUser.PasswordHash = newHash;
        passwordChanged = true;
    }

    // ================= PROFILE UPDATE =================
    WorkerRepository.UpdateProfile(
        _currentUser.Id,
        NameBox.Text.Trim(),
        PhoneBox.Text.Trim(),
        EmailBox.Text.Trim()
    );

    _currentUser.Name = NameBox.Text.Trim();
    _currentUser.Phone = PhoneBox.Text.Trim();
    _currentUser.Email = EmailBox.Text.Trim();

    // ================= CLOSE =================
    DialogResult = passwordChanged;
    Close();
}


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
