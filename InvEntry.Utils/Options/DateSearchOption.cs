using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;

namespace InvEntry.Utils.Options;

public partial class DateSearchOption : ObservableObject
{
    [ObservableProperty]
    private DateTime _From;

    [ObservableProperty]
    private DateTime _To;

    [ObservableProperty]
    private string? _Filter1;

}


