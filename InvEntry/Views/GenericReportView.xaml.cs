using DevExpress.Xpf.WindowsUI;
using DevExpress.Xpf.Grid;
using InvEntry.ViewModels;
using System.Data;
using System.ComponentModel;
using System.Windows;
using System.Runtime.Versioning;

namespace InvEntry.Views
{
    public partial class GenericReportView : NavigationPage
    {
        public GenericReportView()
        {
            InitializeComponent();

            this.DataContextChanged += GenericReportView_DataContextChanged;
        }

        private void GenericReportView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is GenericReportViewModel vm)
                vm.PropertyChanged += Vm_PropertyChanged;
        }

        private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GenericReportViewModel.GenericItems))
            {
                var vm = (GenericReportViewModel)sender;
                BuildDynamicColumns(vm.GenericItems);
            }
        }

        [SupportedOSPlatform("windows")]
        private void btnPrintGrid_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            // Example: Export to Excel
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "Report.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                gridControl.View.ExportToXlsx(dialog.FileName);
            }
        }

        [SupportedOSPlatform("windows")]   //To avoid CA1416
        private void BuildDynamicColumns(DataTable table)
        {
            gridControl.Columns.Clear();
            if (table == null) return;

            foreach (DataColumn col in table.Columns)
            {
                gridControl.Columns.Add(new GridColumn
                {
                    FieldName = col.ColumnName,
                    Header = col.ColumnName,
                    Visible = true
                });
            }
        }
    }
}
