using DevExpress.Xpf.Core;
using InvEntry.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InvEntry.Views;

public partial class Gstr1LineCorrectionView : UserControl
{
    private Gstr1LineCorrectionViewModel? _viewModel;
    private Window? _hostWindow;
    private bool _allowClose;

    public Gstr1LineCorrectionView()
    {
        InitializeComponent();

        DataContextChanged += View_DataContextChanged;
        Loaded += View_Loaded;
        Unloaded += View_Unloaded;
    }

    private void View_Loaded(object sender, RoutedEventArgs e)
    {
        _hostWindow = Window.GetWindow(this);

        if (_hostWindow is null)
            return;

        _hostWindow.Closing += HostWindow_Closing;
        _hostWindow.PreviewKeyDown += HostWindow_PreviewKeyDown;
    }

    private void View_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DetachViewModel();

        _viewModel = e.NewValue as Gstr1LineCorrectionViewModel;

        if (_viewModel is null)
            return;

        _viewModel.Saved += ViewModel_Saved;
        _viewModel.Cancelled += ViewModel_Cancelled;
    }

    private void ViewModel_Saved(object? sender, EventArgs e)
    {
        _allowClose = true;
        CloseWindow(true);
    }

    private void ViewModel_Cancelled(object? sender, EventArgs e) => RequestClose();

    private void HostWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        e.Handled = true;
        RequestClose();
    }

    private void HostWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
            return;

        e.Cancel = true;
        RequestClose();
    }

    private void RequestClose()
    {
        if (_viewModel is null || _viewModel.IsBusy)
            return;

        if (_viewModel.HasChanges &&
            DXMessageBox.Show(
                "The correction has not been saved.\n\nDiscard it and close?",
                "GSTR-1 Line Correction",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        _allowClose = true;
        CloseWindow(false);
    }

    private void CloseWindow(bool? result)
    {
        if (_hostWindow is null)
            return;

        try
        {
            _hostWindow.DialogResult = result;
        }
        catch
        {
            _hostWindow.Close();
        }
    }

    private void View_Unloaded(object sender, RoutedEventArgs e)
    {
        DetachViewModel();

        if (_hostWindow is not null)
        {
            _hostWindow.Closing -= HostWindow_Closing;
            _hostWindow.PreviewKeyDown -= HostWindow_PreviewKeyDown;
        }

        _hostWindow = null;
    }

    private void DetachViewModel()
    {
        if (_viewModel is null)
            return;

        _viewModel.Saved -= ViewModel_Saved;
        _viewModel.Cancelled -= ViewModel_Cancelled;
        _viewModel = null;
    }
}
