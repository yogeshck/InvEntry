using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Models.Settlements;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace InvEntry.ViewModels.Invoices;

public partial class InvoiceSettlementViewModel : ObservableObject
{
    private const decimal BalanceTolerance = 0.01M;

    // =========================================================
    // INVOICE INFORMATION
    // =========================================================

    [ObservableProperty]
    private int invoiceGkey;

    [ObservableProperty]
    private string? invoiceNumber;

    [ObservableProperty]
    private string? customerName;

    [ObservableProperty]
    private string? customerMobile;

    // =========================================================
    // INVOICE / ADJUSTMENT SUMMARY
    // These are display values for the Settlement popup.
    // =========================================================

    /// <summary>
    /// Invoice value before Old Gold / Advance / RD adjustments.
    /// </summary>
    [ObservableProperty]
    private decimal invoiceAmount;

    [ObservableProperty]
    private decimal oldGoldAdjustment;

    [ObservableProperty]
    private decimal oldSilverAdjustment;

    [ObservableProperty]
    private decimal advanceAdjustment;

    [ObservableProperty]
    private decimal rdAdjustment;

    // =========================================================
    // NET SETTLEMENT POSITION
    // =========================================================

    /// <summary>
    /// Signed settlement amount calculated by InvoiceViewModel.
    ///
    /// > 0 : Customer owes shop.
    /// = 0 : Fully adjusted.
    /// < 0 : Shop owes customer (Refund).
    /// </summary>
    [ObservableProperty]
    private decimal netSettlementAmount;

    // =========================================================
    // CREDIT
    // Credit is NOT a receipt.
    // It represents customer receivable.
    // =========================================================

    [ObservableProperty]
    private bool useCredit;

    [ObservableProperty]
    private decimal creditAmount;

    // =========================================================
    // VALIDATION
    // =========================================================

    [ObservableProperty]
    private string? validationMessage;

    // =========================================================
    // COLLECTIONS
    // =========================================================

    /// <summary>
    /// Money received FROM customer.
    /// CASH / UPI / CARD / NEFT / CHEQUE / DD etc.
    /// </summary>
    public ObservableCollection<InvoiceSettlementLine> Receipts { get; }
        = new();

    /// <summary>
    /// Money paid TO customer.
    /// Used when the net settlement position is negative.
    /// </summary>
    public ObservableCollection<InvoiceSettlementLine> Refunds { get; }
        = new();

    // =========================================================
    // SETTLEMENT MODE
    // =========================================================

    public bool IsReceivable =>
        NetSettlementAmount > BalanceTolerance;

    public bool IsRefund =>
        NetSettlementAmount < -BalanceTolerance;

    public bool IsFullyAdjusted =>
        Math.Abs(NetSettlementAmount) <= BalanceTolerance;

    // =========================================================
    // RECEIVABLE
    // =========================================================

    public decimal ReceivableAmount =>
        IsReceivable
            ? NetSettlementAmount
            : 0M;

    public decimal TotalReceived =>
        Receipts.Sum(x => x.Amount);

    public decimal EffectiveCreditAmount =>
        IsReceivable && UseCredit
            ? CreditAmount
            : 0M;

    /// <summary>
    /// Amount still to be settled by the customer.
    ///
    /// Positive = still due.
    /// Zero     = fully settled.
    /// Negative = receipt entered in excess.
    /// </summary>
    public decimal ReceivableBalance =>
        ReceivableAmount
        - TotalReceived
        - EffectiveCreditAmount;

    public bool HasReceivableShortfall =>
        IsReceivable &&
        ReceivableBalance > BalanceTolerance;

    public bool HasReceiptExcess =>
        IsReceivable &&
        ReceivableBalance < -BalanceTolerance;

    // =========================================================
    // REFUND
    // =========================================================

    public decimal RefundPayable =>
        IsRefund
            ? Math.Abs(NetSettlementAmount)
            : 0M;

    public decimal TotalRefunded =>
        Refunds.Sum(x => x.Amount);

    /// <summary>
    /// Amount still payable by shop to customer.
    ///
    /// Positive = refund still due.
    /// Zero     = refund fully settled.
    /// Negative = refund entered in excess.
    /// </summary>
    public decimal RefundBalance =>
        RefundPayable - TotalRefunded;

    public bool HasRefundShortfall =>
        IsRefund &&
        RefundBalance > BalanceTolerance;

    public bool HasRefundExcess =>
        IsRefund &&
        RefundBalance < -BalanceTolerance;

    // =========================================================
    // OVERALL STATUS
    // =========================================================

    public bool IsSettlementComplete
    {
        get
        {
            if (IsFullyAdjusted)
                return true;

            if (IsReceivable)
            {
                return Math.Abs(ReceivableBalance)
                       <= BalanceTolerance;
            }

            if (IsRefund)
            {
                return Math.Abs(RefundBalance)
                       <= BalanceTolerance;
            }

            return false;
        }
    }

    public bool CanFinalise =>
        AreSettlementLinesValid() &&
        IsSettlementComplete;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public InvoiceSettlementViewModel()
    {
        Receipts.CollectionChanged +=
            Receipts_CollectionChanged;

        Refunds.CollectionChanged +=
            Refunds_CollectionChanged;
    }

    // =========================================================
    // RECEIPT COMMANDS
    // =========================================================

    [RelayCommand]
    private void AddReceipt()
    {
        if (!IsReceivable)
            return;

        var receipt = new InvoiceSettlementLine
        {
            PaymentMode = "CASH",
            TransactionDate = DateTime.Today
        };

        Receipts.Add(receipt);
    }

    [RelayCommand]
    private void RemoveReceipt(
        InvoiceSettlementLine? receipt)
    {
        if (receipt is null)
            return;

        Receipts.Remove(receipt);
    }

    // =========================================================
    // REFUND COMMANDS
    // =========================================================

    [RelayCommand]
    private void AddRefund()
    {
        if (!IsRefund)
            return;

        var refund = new InvoiceSettlementLine
        {
            PaymentMode = "CASH",
            TransactionDate = DateTime.Today
        };

        Refunds.Add(refund);
    }

    [RelayCommand]
    private void RemoveRefund(
        InvoiceSettlementLine? refund)
    {
        if (refund is null)
            return;

        Refunds.Remove(refund);
    }

    // =========================================================
    // CREDIT
    // =========================================================

    partial void OnUseCreditChanged(bool value)
    {
        if (!value)
        {
            CreditAmount = 0M;
        }
        else if (IsReceivable)
        {
            var remaining =
                ReceivableAmount - TotalReceived;

            CreditAmount =
                remaining > 0M
                    ? remaining
                    : 0M;
        }

        Recalculate();
    }

    partial void OnCreditAmountChanged(decimal value)
    {
        Recalculate();
    }

    // =========================================================
    // NET SETTLEMENT CHANGED
    // =========================================================

    partial void OnNetSettlementAmountChanged(
        decimal value)
    {
        /*
         * Net settlement direction has changed.
         *
         * We deliberately clear incompatible settlement data.
         *
         * Receivable:
         *      customer -> shop
         *
         * Refund:
         *      shop -> customer
         */

        if (IsReceivable)
        {
            Refunds.Clear();
        }
        else if (IsRefund)
        {
            Receipts.Clear();

            UseCredit = false;
            CreditAmount = 0M;
        }
        else
        {
            Receipts.Clear();
            Refunds.Clear();

            UseCredit = false;
            CreditAmount = 0M;
        }

        Recalculate();
    }

    // =========================================================
    // COLLECTION EVENTS
    // =========================================================

    private void Receipts_CollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        UnsubscribeOldItems(e);
        SubscribeNewItems(e);

        Recalculate();
    }

    private void Refunds_CollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        UnsubscribeOldItems(e);
        SubscribeNewItems(e);

        Recalculate();
    }

    private void SubscribeNewItems(
        NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is null)
            return;

        foreach (InvoiceSettlementLine line
                 in e.NewItems)
        {
            line.PropertyChanged +=
                SettlementLine_PropertyChanged;
        }
    }

    private void UnsubscribeOldItems(
        NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is null)
            return;

        foreach (InvoiceSettlementLine line
                 in e.OldItems)
        {
            line.PropertyChanged -=
                SettlementLine_PropertyChanged;
        }
    }

    private void SettlementLine_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        Recalculate();
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private bool AreSettlementLinesValid()
    {
        if (Receipts.Any(x =>
                x.Amount <= 0M ||
                string.IsNullOrWhiteSpace(
                    x.PaymentMode)))
        {
            return false;
        }

        if (Refunds.Any(x =>
                x.Amount <= 0M ||
                string.IsNullOrWhiteSpace(
                    x.PaymentMode)))
        {
            return false;
        }

        if (CreditAmount < 0M)
            return false;

        return true;
    }

    private void ValidateSettlement()
    {
        ValidationMessage = null;

        // -----------------------------------------------------
        // Fully adjusted
        // -----------------------------------------------------

        if (IsFullyAdjusted)
        {
            ValidationMessage = null;
            return;
        }

        // -----------------------------------------------------
        // Customer owes shop
        // -----------------------------------------------------

        if (IsReceivable)
        {
            if (Receipts.Any(x => x.Amount <= 0M))
            {
                ValidationMessage =
                    "Receipt amount must be greater than zero.";

                return;
            }

            if (Receipts.Any(x =>
                    string.IsNullOrWhiteSpace(
                        x.PaymentMode)))
            {
                ValidationMessage =
                    "Please select a receipt mode.";

                return;
            }

            if (CreditAmount < 0M)
            {
                ValidationMessage =
                    "Credit amount cannot be negative.";

                return;
            }

            if (HasReceiptExcess)
            {
                ValidationMessage =
                    $"Receipt exceeds the amount receivable by " +
                    $"{Math.Abs(ReceivableBalance):N2}.";

                return;
            }

            if (HasReceivableShortfall)
            {
                ValidationMessage =
                    $"Amount {ReceivableBalance:N2} is still receivable.";

                return;
            }

            return;
        }

        // -----------------------------------------------------
        // Shop owes customer
        // -----------------------------------------------------

        if (IsRefund)
        {
            if (Refunds.Any(x => x.Amount <= 0M))
            {
                ValidationMessage =
                    "Refund amount must be greater than zero.";

                return;
            }

            if (Refunds.Any(x =>
                    string.IsNullOrWhiteSpace(
                        x.PaymentMode)))
            {
                ValidationMessage =
                    "Please select a refund mode.";

                return;
            }

            if (HasRefundExcess)
            {
                ValidationMessage =
                    $"Refund exceeds the amount payable by " +
                    $"{Math.Abs(RefundBalance):N2}.";

                return;
            }

            if (HasRefundShortfall)
            {
                ValidationMessage =
                    $"Refund amount {RefundBalance:N2} is still payable.";

                return;
            }
        }
    }

    // =========================================================
    // RECALCULATION
    // =========================================================

    private void Recalculate()
    {
        OnPropertyChanged(nameof(IsReceivable));
        OnPropertyChanged(nameof(IsRefund));
        OnPropertyChanged(nameof(IsFullyAdjusted));

        OnPropertyChanged(nameof(ReceivableAmount));
        OnPropertyChanged(nameof(TotalReceived));
        OnPropertyChanged(nameof(EffectiveCreditAmount));
        OnPropertyChanged(nameof(ReceivableBalance));

        OnPropertyChanged(nameof(HasReceivableShortfall));
        OnPropertyChanged(nameof(HasReceiptExcess));

        OnPropertyChanged(nameof(RefundPayable));
        OnPropertyChanged(nameof(TotalRefunded));
        OnPropertyChanged(nameof(RefundBalance));

        OnPropertyChanged(nameof(HasRefundShortfall));
        OnPropertyChanged(nameof(HasRefundExcess));

        OnPropertyChanged(nameof(IsSettlementComplete));

        ValidateSettlement();

        OnPropertyChanged(nameof(CanFinalise));
    }
}