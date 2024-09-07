using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Tern.MI.InvEntry.Models;

public partial class BaseEntity : ObservableObject
{
    [ObservableProperty]
    private long id;

    [ObservableProperty]
    private DateTime createdOn;

    [ObservableProperty]
    private string createdBy;

    [ObservableProperty]
    private DateTime modifiedOn;

    [ObservableProperty]
    private string modifiedBy;
}