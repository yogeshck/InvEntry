using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace InvEntry.ViewModels;

/*public class CustomerOrderEditViewModel : INotifyPropertyChanged
{
    private readonly CustomerOrderLine _orderLine;

    public int ProdQty
    {
        get => _orderLine.ProdQty;
        set { _orderLine.ProdQty = value; OnPropertyChanged(); }
    }

    public List<string> StatusOptions { get; } = new() { "New", "In Progress", "Completed" };
    public string SelectedStatus
    {
        get => _orderLine.orderItemStatusFlag;
        set { _orderLine.OrderStatus = value; OnPropertyChanged(); }
    }

    public ICommand SaveCommand { get; }

    public EditOrderLineViewModel(CustomerOrderDBView orderLine)
    {
        _orderLine = orderLine;
        SaveCommand = new RelayCommand(OnSave);
    }

    private void OnSave()
    {
        // Save logic here (DB update or signal parent VM)
        CloseWindow();
    }

    private void CloseWindow()
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window.DataContext == this)
            {
                window.Close();
                break;
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string prop = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}*/

public class CustomerOrderEditViewModel : ViewModelBase
{
    private CustomerOrderLine _orderLine;

    public void Initialize(CustomerOrderLine orderLine)
    {
        _orderLine = orderLine;
        ProdQty = orderLine.ProdQty;
        //SelectedStatus = orderLine.orderItemStatusFlag;
    }

    public int ProdQty { get; set; }
    public string SelectedStatus { get; set; }

    public ICommand SaveCommand => new RelayCommand(Save);

    private void Save()
    {
        _orderLine.ProdQty = ProdQty;
        //_orderLine.OrderStatus = SelectedStatus;

        // Save changes to database if needed
        CloseWindow();
    }

    private void CloseWindow()
    {
        foreach (Window win in Application.Current.Windows)
        {
            if (win.DataContext == this)
                win.Close();
        }
    }
}

