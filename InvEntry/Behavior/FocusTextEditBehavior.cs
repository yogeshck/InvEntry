using DevExpress.Mvvm;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Editors;
using InvEntry.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Behavior;

public class FocusTextEditBehavior : Behavior<TextEdit>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        Messenger.Default.Register<string>(this, MessageType.FocusTextEdit, SetFocus);
    }

    protected override void OnDetaching()
    {
        Messenger.Default.Unregister<string>(this, MessageType.FocusTextEdit, SetFocus);
        base.OnDetaching();
    }

    private void SetFocus(string elementName)
    {
        if (string.IsNullOrEmpty(elementName)) return;

        if (AssociatedObject.Name.Equals(elementName, StringComparison.OrdinalIgnoreCase))
        {
            AssociatedObject.Focus();
        }
    }
}
