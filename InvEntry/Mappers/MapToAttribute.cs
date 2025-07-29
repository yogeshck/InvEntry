using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Mappers
{

    [AttributeUsage(AttributeTargets.Property)]
    public class MapToAttribute : Attribute
    {
        public string DbField { get; }
        public MapToAttribute(string dbField) => DbField = dbField;
    }


}
