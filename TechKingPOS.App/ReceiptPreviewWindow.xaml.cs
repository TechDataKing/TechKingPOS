using System.Windows;

namespace TechKingPOS.App
{
    public partial class ReceiptPreviewWindow : Window
    {
        public ReceiptPreviewWindow(string receipt)
        {
            InitializeComponent();
            ReceiptText.Text = receipt;
        }
    }
}
