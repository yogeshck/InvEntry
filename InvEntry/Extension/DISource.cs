using DevExpress.Mvvm.UI.Native;
using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace InvEntry.Extension;

public class DISource : MarkupExtension
{
    public static Func<Type, object> Resolver { get; set; }

    public Type Type { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider) => Resolver.Invoke(Type);

    public static object Resolve(Type type)
    {
        var diSource = new DISource()
        {
            Type = type
        };

        return diSource.ProvideValue(null);
    }

    public static T Resolve<T>()
    {
        return (T)Resolve(typeof(T));
    }
}