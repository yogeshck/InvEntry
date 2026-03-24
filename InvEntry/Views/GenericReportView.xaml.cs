using InvEntry.ViewModels;
using System.ComponentModel;
using System.Windows.Controls;
using DevExpress.Xpf.Grid;
using System.Data;
using System.Windows;


namespace InvEntry.Views
{
    /// <summary>
    /// Interaction logic for GenericReportView.xaml
    /// </summary>
    public partial class GenericReportView : UserControl
    {
        public GenericReportView()
        {
            InitializeComponent();

            // Listen for DataContext changes
            this.DataContextChanged += GenericReportView_DataContextChanged;
        }

        private void GenericReportView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is GenericReportViewModel vm)
            {
                vm.PropertyChanged += Vm_PropertyChanged;
            }
        }

        private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GenericReportViewModel.GenericItems))
            {
                var vm = (GenericReportViewModel)sender;
                BuildDynamicColumns(vm.GenericItems);
            }
        }

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
