using System.Windows;
using TechKingPOS.App.Data;
using TechKingPOS.App.Models;

namespace TechKingPOS.App
{
    public partial class AddBranchWindow : Window
    {
        public AddBranchWindow()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Branch name is required.");
                return;
            }

            BranchRepository.Insert(
                NameBox.Text.Trim(),
                CodeBox.Text.Trim(),
                ActiveRadio.IsChecked == true
            );

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
