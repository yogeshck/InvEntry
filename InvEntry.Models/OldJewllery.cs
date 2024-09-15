using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models;

public partial class OldJewllery : ObservableObject
{
    [ObservableProperty]
    private MetalType metalType;

    [ObservableProperty]
    private double? weight;

    [ObservableProperty]
    private double? amount;
}

public enum MetalType
{
    Gold,
    Silver,
    Diamond
}