using DevExpress.Export;
using DevExpress.Printing.ExportHelpers;
using DevExpress.XtraPrinting;
using InvEntry.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace InvEntry.Views
{
    /// <summary>
    /// RD dues and collections for a month. Excel export lives here because it
    /// exports the grid as shown (filters, sorting and grouping applied by the user).
    /// </summary>
    public partial class RdDuesView : UserControl
    {
        public RdDuesView()
        {
            InitializeComponent();
        }

        private void btnExportExcel_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if (DataContext is not RdDuesViewModel vm)
                return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = vm.FileBaseName + ".xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            var title = $"RD {vm.Filter} — {vm.MonthTitle}" + (vm.IncludeArrears ? " (with arrears)" : string.Empty);

            var options = new XlsxExportOptionsEx
            {
                ExportType = ExportType.DataAware,
                SheetName = vm.SelectedMonth.ToString("MMM yyyy"),
                ShowGridLines = true
            };
            options.CustomizeSheetHeader += context =>
            {
                context.ExportContext.AddRow(new[] { new CellObject { Value = title } });
                context.ExportContext.AddRow(new[] { new CellObject { Value = $"Generated {DateTime.Now:dd-MM-yyyy HH:mm}" } });
                context.ExportContext.AddRow();
            };

            try
            {
                tableView.ExportToXlsx(dialog.FileName, options);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Window.GetWindow(this), "The Excel file could not be saved." + Environment.NewLine + ex.Message,
                    "Export Excel", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show(Window.GetWindow(this), $"Saved {dialog.FileName}.{Environment.NewLine}Open it now?",
                    "Export Excel", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
        }
    }
}
