using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Behavior;

public class BestFitGridBehavior : Behavior<TableView>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Loaded += AssociatedObject_Loaded;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.Loaded -= AssociatedObject_Loaded;
    }

    private void AssociatedObject_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        AssociatedObject.BestFitColumns();
    }
}
