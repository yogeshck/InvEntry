using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DependencyPropertyGenerator;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Accordion;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Grid;

namespace InvEntry.Behavior;

[DependencyProperty<BarButtonItem>("BarButtonItem")]
public partial class PrintGridBehavior : Behavior<GridControl>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        BarButtonItem.ItemClick += BarButtonItem_ItemClick;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        BarButtonItem.ItemClick -= BarButtonItem_ItemClick;
    }

    private void BarButtonItem_ItemClick(object sender, ItemClickEventArgs e)
    {
        AssociatedObject.View.ShowPrintPreviewDialog(Application.Current.MainWindow);
    }
}
