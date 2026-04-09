using DevExpress.Xpf.Grid;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InvEntry.Views
{
    /// <summary>
    /// Interaction logic for DailyStockEntryView.xaml
    /// </summary>
    public partial class DailyStockEntryView : UserControl
    {
        public DailyStockEntryView()
        {
            InitializeComponent();
        }

        private void TableView_ShowingEditor(object sender, ShowingEditorEventArgs e)
        {
            var row = e.Row as DailyStockSummary;
            if (row == null) return;

            // Block editing for Opening fields if IsObEditable is false
            if ((e.Column.FieldName == "OpeningStockQty" || e.Column.FieldName == "OpeningStockNetWeight")
                && !row.IsObEditable)
            {
                e.Cancel = true; // prevents editor from opening
            }
        }

    }
}
