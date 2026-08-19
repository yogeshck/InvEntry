using DevExpress.Xpf.Core;
using InvEntry.ViewModels.Common;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InvEntry.Views.Common;

public partial class CustomerEditView : UserControl
{
    private CustomerEditViewModel? _viewModel;
    private Window? _hostWindow;

    private bool _allowClose;

    public CustomerEditView()
    {
        InitializeComponent();

        DataContextChanged +=
            CustomerEditView_DataContextChanged;

        Loaded +=
            CustomerEditView_Loaded;

        Unloaded +=
            CustomerEditView_Unloaded;
    }

    private void CustomerEditView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        _hostWindow =
            Window.GetWindow(this);

        if (_hostWindow is null)
            return;

        _hostWindow.Closing +=
            HostWindow_Closing;

        _hostWindow.PreviewKeyDown +=
            HostWindow_PreviewKeyDown;
    }

    private void CustomerEditView_DataContextChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        DetachViewModel();

        _viewModel =
            e.NewValue as CustomerEditViewModel;

        if (_viewModel is null)
            return;

        _viewModel.Saved +=
            ViewModel_Saved;

        _viewModel.Cancelled +=
            ViewModel_Cancelled;
    }

    private void ViewModel_Saved(
        object? sender,
        CustomerSavedEventArgs e)
    {
        _allowClose = true;

        CloseWindow(true);
    }

    private void ViewModel_Cancelled(
        object? sender,
        EventArgs e)
    {
        RequestClose();
    }

    private void HostWindow_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        e.Handled = true;

        RequestClose();
    }

    private void HostWindow_Closing(
        object? sender,
        CancelEventArgs e)
    {
        if (_allowClose)
            return;

        e.Cancel = true;

        RequestClose();
    }

    private void RequestClose()
    {
        if (_viewModel is null)
            return;

        //
        // Nothing changed.
        //
        if (!_viewModel.HasChanges)
        {
            _allowClose = true;

            CloseWindow(false);

            return;
        }

        var result =
            DXMessageBox.Show(
                "Customer details have been changed.\n\n" +
                "Do you want to save the changes before closing?",
                "Unsaved Customer Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

        switch (result)
        {
            case MessageBoxResult.Yes:

                //
                // Don't close yet.
                //
                // SaveCommand performs validation/persistence.
                // Successful save raises Saved and closes the dialog.
                //
                if (_viewModel.SaveCommand.CanExecute(null))
                {
                    _viewModel.SaveCommand.Execute(null);
                }

                break;

            case MessageBoxResult.No:

                //
                // Explicit discard.
                //
                _allowClose = true;

                CloseWindow(false);

                break;

            case MessageBoxResult.Cancel:

                //
                // Stay in editor.
                //
                break;
        }
    }

    private void CloseWindow(
        bool? result)
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

    private void CustomerEditView_Unloaded(
        object sender,
        RoutedEventArgs e)
    {
        DetachViewModel();

        if (_hostWindow is not null)
        {
            _hostWindow.Closing -=
                HostWindow_Closing;

            _hostWindow.PreviewKeyDown -=
                HostWindow_PreviewKeyDown;
        }

        _hostWindow = null;
    }

    private void DetachViewModel()
    {
        if (_viewModel is null)
            return;

        _viewModel.Saved -=
            ViewModel_Saved;

        _viewModel.Cancelled -=
            ViewModel_Cancelled;

        _viewModel = null;
    }
}