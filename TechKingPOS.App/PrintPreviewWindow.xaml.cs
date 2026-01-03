using System.Windows;
using System.Windows.Documents;
using System.Printing;
using System.Windows.Controls;


namespace TechKingPOS.App
{
    public partial class PrintPreviewWindow : Window
    {
        private readonly FlowDocument _document;

        public PrintPreviewWindow(FlowDocument document)
        {
            InitializeComponent();

            _document = document;
            PreviewViewer.Document = document; // ✅ VALID
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PrintDialog();

            if (dialog.ShowDialog() == true)
            {
                dialog.PrintDocument(
                    ((IDocumentPaginatorSource)_document).DocumentPaginator,
                    "TechKingPOS Report"
                );
            }
        }
    }
}
