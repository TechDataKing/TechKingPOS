using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TechKingPOS.App.Data;
using TechKingPOS.App.Models;

namespace TechKingPOS.App
{
    public partial class WorkerWindow : Window
    {
        private List<WorkerView> _allWorkers = new();

        public WorkerWindow()
        {
            InitializeComponent();
            LoadWorkers();
        }

        private void LoadWorkers()
        {
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

            WorkerRepository.Insert(
                NameBox.Text.Trim(),
                NationalIdBox.Text.Trim(),
                PhoneBox.Text.Trim(),
                EmailBox.Text.Trim()
            );

            NameBox.Clear();
            NationalIdBox.Clear();
            PhoneBox.Clear();
            EmailBox.Clear();

            LoadWorkers();
        }

        private void Activate_Click(object sender, RoutedEventArgs e)
        {
            if (WorkersGrid.SelectedItem is not WorkerView worker)
                return;

            WorkerRepository.SetActive(worker.Id, true);
            LoadWorkers();
        }

        private void Deactivate_Click(object sender, RoutedEventArgs e)
        {
            if (WorkersGrid.SelectedItem is not WorkerView worker)
                return;

            WorkerRepository.SetActive(worker.Id, false);
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
    }
}
