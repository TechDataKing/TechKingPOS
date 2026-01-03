using System.Windows;
using TechKingPOS.App.Models;
using TechKingPOS.App.Data;

namespace TechKingPOS.App
{
    public partial class EditBranchWindow : Window
    {
        private readonly Branch _branch;

        public EditBranchWindow(Branch branch)
        {
            InitializeComponent();
            _branch = branch;

            NameBox.Text = branch.Name;
            CodeBox.Text = branch.Code;

            if (branch.IsActive)
                ActiveRadio.IsChecked = true;
            else
                InactiveRadio.IsChecked = true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            BranchRepository.Update(
                _branch.Id,
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
