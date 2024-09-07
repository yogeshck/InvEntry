using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tern.MI.InvEntry.Models;

namespace InvEntry.ViewModels
{
    public partial class InvoiceViewModel : ObservableObject
    {
        [ObservableProperty]
        private InvoiceHeader _header;

        public InvoiceViewModel()
        {
            Header = new()
            {
                InvDate = DateTime.Now,
                InvNbr = InvoiceNumberGenerator.Generate()
            };
        }
    }
}
