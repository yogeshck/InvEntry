using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace InvEntry.Models.Settlements;

public partial class InvoiceSettlementLine : ObservableObject
{
    /// <summary>
    /// CASH, UPI, CARD, NEFT, IMPS, RTGS, CHEQUE, DD, etc.
    /// </summary>
    [ObservableProperty]
    private string? paymentMode;

    /// <summary>
    /// Amount received through this payment mode.
    /// </summary>
    [ObservableProperty]
    private decimal amount;

    /// <summary>
    /// Electronic transaction identifier.
    /// Example: UPI transaction ID, UTR, card transaction ID.
    /// </summary>
    [ObservableProperty]
    private string? transactionId;

    /// <summary>
    /// Date of the electronic transaction.
    /// </summary>
    [ObservableProperty]
    private DateTime? transactionDate;

    /// <summary>
    /// Cheque number, DD number or similar instrument number.
    /// </summary>
    [ObservableProperty]
    private string? instrumentNumber;

    /// <summary>
    /// Cheque date, DD date, etc.
    /// May be a future date.
    /// </summary>
    [ObservableProperty]
    private DateTime? instrumentDate;

    /// <summary>
    /// Customer/sender/issuing bank where applicable.
    /// </summary>
    [ObservableProperty]
    private string? bankName;

    /// <summary>
    /// Company's receiving bank account where applicable.
    /// </summary>
    [ObservableProperty]
    private string? companyBankAccountNbr;

    /// <summary>
    /// Optional additional payment reference or remarks.
    /// </summary>
    [ObservableProperty]
    private string? otherReference;
}