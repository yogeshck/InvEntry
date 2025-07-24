using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Models;

namespace InvEntry.ViewModels
{

    public partial class ReviewPopupViewModel : ObservableObject
    {
        public InvoiceHeader Invoice { get; set; }
        public bool Confirmed { get; set; }

        public ReviewPopupViewModel(InvoiceHeader invoice)
        {
            Invoice = invoice;
        }
        
    }

    
}
