using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace InvEntry.Utils;

public class DISource : MarkupExtension
{
    private static DISource instance;

    public static Func<Type, object, string, object> Resolver { get; set; }

    public Type Type { get; set; }
    public object Key { get; set; }
    public string Name { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider) => Resolver?.Invoke(Type, Key, Name);

    public static T? ProvideValue<T>()
    {
        if (instance is null) instance = new();

        return (T)instance.ProvideValue(null);
    }
}
