﻿using System;
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
    /// Interaction logic for DailyStockSummaryListView.xaml
    /// </summary>
    public partial class DailyStockSummaryListView : UserControl
    {
        public DailyStockSummaryListView()
        {
            InitializeComponent();
        }

        private void btnPrint_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            //PrintHelper.ShowPrintPreviewDialog(this,);
            //VouchersTableview.ShowPrintPreviewDialog(Application.Current.MainWindow);
            DailyStkSumryTblView.ShowRibbonPrintPreviewDialog(Application.Current.MainWindow);
        }
    }
}
