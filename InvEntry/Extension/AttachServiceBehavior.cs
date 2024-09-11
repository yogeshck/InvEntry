using DevExpress.Mvvm.UI.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DependencyPropertyGenerator;
using DevExpress.Mvvm.UI;

namespace InvEntry.Extension;

[DependencyProperty<ServiceBase>("AtachableService")]
public partial class AttachServiceBehavior : Behavior<DependencyObject>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AttachService();
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AtachableService?.Detach();
    }

    void AttachService()
    {
        if (AtachableService == null || AssociatedObject == null)
            return;
        if (AtachableService.IsAttached)
            AtachableService.Detach();
        AtachableService.Attach(AssociatedObject);
    }
}
