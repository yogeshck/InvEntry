using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class Customer : BaseEntity
    {
        public Customer()
        {
            address = new();
        }

        [ObservableProperty]
        private OrgAddress address;

        [ObservableProperty]
        private string mobileNbr;

        [ObservableProperty]
        private string customerName;

        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string ledgerName;

        [ObservableProperty]
        private string gstStateCode;

        
    }
}
