using DevExpress.Xpf.Core;
using System.Windows.Controls;
using DevExpress.Xpf.Editors;
using System.Windows;

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

    }
}
