using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tern.MI.InvEntry.Models;

namespace InvEntry.ViewModels
{
    public class Customer
    {
        public string Mobile {  get; set; }
        public string Name { get; set; }
        public string Email { get; set; } 
        public string Address { get; set; } 
    }

    public partial class InvoiceViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Customer> _customers;

        [ObservableProperty]
        private string _custMobile;

        [ObservableProperty]
        private InvoiceHeader _header;

        public InvoiceViewModel()
        {
            Customers = new ObservableCollection<Customer>();

            Customers.Add(new Customer() { Mobile = "9841051171", Name="Kumaravel", Address= "mylapore" });
            Customers.Add(new Customer() { Mobile = "0410060197", Name="Yogesh", Address= "sydney" });

            Header = new()
            {
                InvDate = DateTime.Now,
                InvNbr = InvoiceNumberGenerator.Generate(),
                Lines = new()
            };
        }


    }
}
