using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Helpers;

public static class AuditHelper
{
    public const string CurrentUser = "System";

    public static DateTime Now =>
        DateTime.Now;
}