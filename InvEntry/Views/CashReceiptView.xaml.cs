using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InvEntry.Views
{
    /// <summary>
    /// Interaction logic for CashReceiptView.xaml
    /// </summary>
    public partial class CashReceiptView : UserControl
    {
        public CashReceiptView()
        {
            InitializeComponent();
        }

        private void NotesTextEdit_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var textEdit = sender as DevExpress.Xpf.Editors.TextEdit;
            if (textEdit != null && textEdit.Text.Length >= 50)
            {

                DXMessageBox.Show(
                    "Maximum 50 characters are allowed in Notes.",
                    "Character Limit Reached",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InvoiceCombo?.ClosePopup();
            // Move focus to the amount field
            //AmountField?.Focus();

        }

        private void InvoiceCombo_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var combo = sender as ComboBoxEdit;
            if (combo != null && !combo.IsPopupOpen)
            {
                combo.ShowPopup();
                e.Handled = true;
            }
        }


    }
}
