using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace InvEntry.ViewModels
{
    public partial class ReceiptAccountingViewModel : ObservableObject
    {

        [ObservableProperty]
        private string _extTransactionRefId;

        [ObservableProperty]
        private string _chequeNumber;

        [ObservableProperty]
        private string _bankName;

        [ObservableProperty]
        private string _iFSCCode;

        //public bool IsUPI => Transaction.TransactionType == "UPI";
        //public bool IsBank => Transaction.TransactionType == "Bank";

        private Dictionary<string, List<string>> _errors = new();

        public bool HasErrors => _errors.Any();

        public IEnumerable GetErrors(string propertyName)
        {
            if (propertyName != null && _errors.ContainsKey(propertyName))
                return _errors[propertyName];
            return null;
        }


        public ReceiptAccountingViewModel(InvoiceArReceipt invArRct)
        {
            //nvArRct
            Validate(invArRct);
        }

        [RelayCommand]
        private void Save()
        {
            // Close dialog and persist changes
            //validate
            //then return;

        }

        public void Validate(InvoiceArReceipt invArRct)
        {
            _errors.Clear();

            if (invArRct.TransactionType == "UPI" && string.IsNullOrWhiteSpace(ExtTransactionRefId))
            {
                AddError(nameof(ExtTransactionRefId), "Transaction Id is required for UPI.");
            }

/*            if (TransactionType == "Bank")
            {
                if (string.IsNullOrWhiteSpace(ChequeNumber))
                    AddError(nameof(ChequeNumber), "Cheque Number is required.");
                if (!int.TryParse(ChequeNumber, out _))
                    AddError(nameof(ChequeNumber), "Cheque Number must be numeric.");
                if (string.IsNullOrWhiteSpace(BankName))
                    AddError(nameof(BankName), "Bank Name is required.");
                if (string.IsNullOrWhiteSpace(IFSCCode) || IFSCCode.Length != 11)
                    AddError(nameof(IFSCCode), "IFSC must be 11 characters.");
            }*/

            RaiseErrorsChanged();
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();
            _errors[propertyName].Add(error);
        }

        private void RaiseErrorsChanged()
        {
            //foreach (var prop in _errors.Keys)
              //  ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(prop));
        }

    }
}
