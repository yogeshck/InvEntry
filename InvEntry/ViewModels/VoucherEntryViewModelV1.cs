using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace InvEntry.ViewModels
{
    public partial class VoucherEntryViewModelV1 : ObservableObject
    {

        [ObservableProperty]
        private string invoiceNumber;

        [ObservableProperty]
        private Customer selectedCustomer;

        public ObservableCollection<Customer> Customers { get; set; }
        public ObservableCollection<Voucher> Vouchers { get; set; }

        public ICommand AddVoucherCommand { get; }
        public ICommand SaveCommand { get; }

        public VoucherEntryViewModelV1()
        {
           //To do Customers = LoadCustomers(); // fetch from DB
            Vouchers = new ObservableCollection<Voucher>();
         //   AddVoucherCommand = new RelayCommand(AddVoucher);
            SaveCommand = new AsyncRelayCommand(SaveVouchersAsync);
        }

        private void AddVoucher(object obj)
        {
            Vouchers.Add(new Voucher { VoucherType = "Cash", TransAmount = 0 });
        }

        private async Task SaveVouchersAsync()
        {
          //  int invoiceId = await FindInvoiceIdAsync(invoiceNumber);
            foreach (var voucher in Vouchers)
            {
                voucher.RefDocNbr = "TEst";
              //  voucher.CustomerId = selectedCustomer.CustomerId;
                voucher.VoucherDate = DateTime.Now;
            }

           // await VoucherService.SaveVouchersAsync(Vouchers);
            Vouchers.Clear();
        }
    }
}
