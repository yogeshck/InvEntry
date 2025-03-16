using DevExpress.Xpf.Grid;
using DevExpress.Xpf.WindowsUI;
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
    /// Interaction logic for StockSummaryView.xaml
    /// </summary>
    public partial class ProductStockSummaryListView : NavigationPage
    {
        public ProductStockSummaryListView()
        {
            InitializeComponent();
        }

        private void btnPrint_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            //PrintHelper.ShowPrintPreviewDialog(this,);
            //VouchersTableview.ShowPrintPreviewDialog(Application.Current.MainWindow);
            tableView.ShowRibbonPrintPreviewDialog(Application.Current.MainWindow);
        }
    }
}
