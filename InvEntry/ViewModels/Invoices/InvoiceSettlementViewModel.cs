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
    // DISCOUNT
    // =========================================================

    /// <summary>
    /// Discount entered during final settlement.
    ///
    /// Discount is NOT a payment mode.
    /// It reduces the amount that has to be settled.
    /// </summary>
    [ObservableProperty]
    private decimal discountAmount;


    // =========================================================
    // NET SETTLEMENT POSITION
    // =========================================================

    /// <summary>
    /// Signed settlement amount calculated by InvoiceViewModel
    /// before the settlement-stage discount.
    ///
    /// > 0 : Customer owes shop.
    /// = 0 : Fully adjusted.
    /// < 0 : Shop owes customer (Refund).
    /// </summary>
    [ObservableProperty]
    private decimal netSettlementAmount;


    /// <summary>
    /// Final settlement position after applying discount.
    ///
    /// For a positive receivable, Discount reduces the amount
    /// payable by the customer.
    ///
    /// We deliberately do not use Discount to increase an
    /// already-negative refund position.
    /// </summary>
    public decimal AmountAfterDiscount =>
        NetSettlementAmount > 0M
            ? Math.Max(
                0M,
                NetSettlementAmount - DiscountAmount)
            : NetSettlementAmount;


    /// <summary>
    /// Discount cannot be negative and cannot exceed a
    /// positive settlement amount.
    /// </summary>
    public bool IsDiscountValid =>
        DiscountAmount >= 0M &&
        (
            NetSettlementAmount <= 0M ||
            DiscountAmount <= NetSettlementAmount
        );


    // =========================================================
    // CREDIT
    // =========================================================

    /// <summary>
    /// Credit is NOT a receipt.
    /// It represents the amount left outstanding against
    /// the customer.
    /// </summary>
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
    // PAYMENT MODES
    // =========================================================

    /// <summary>
    /// Reference-table driven list of payment modes.
    ///
    /// Examples:
    /// Cash
    /// Bank
    /// Credit Card
    /// GPAY
    /// Advance Adj
    /// RD Adj
    /// etc.
    /// </summary>
    public ObservableCollection<string> PaymentModes { get; }
        = new();


    // =========================================================
    // COLLECTIONS
    // =========================================================

    /// <summary>
    /// Settlement received FROM customer.
    ///
    /// Normal payment modes represent money received.
    /// Advance Adj / RD Adj are adjustments and will be
    /// interpreted appropriately by the backend.
    /// </summary>
    public ObservableCollection<InvoiceSettlementLine> Receipts { get; }
        = new();


    /// <summary>
    /// Money paid TO customer.
    ///
    /// Used when the final settlement position is negative.
    /// </summary>
    public ObservableCollection<InvoiceSettlementLine> Refunds { get; }
        = new();


    // =========================================================
    // SETTLEMENT DIRECTION
    // =========================================================

    public bool IsReceivable =>
        AmountAfterDiscount > BalanceTolerance;


    public bool IsRefund =>
        AmountAfterDiscount < -BalanceTolerance;


    public bool IsFullyAdjusted =>
        Math.Abs(AmountAfterDiscount)
        <= BalanceTolerance;


    // =========================================================
    // RECEIVABLE
    // =========================================================

    public decimal ReceivableAmount =>
        IsReceivable
            ? AmountAfterDiscount
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

    /// <summary>
    /// Amount that must be explicitly refunded to customer.
    /// </summary>
    public decimal RefundPayable =>
        IsRefund
            ? Math.Abs(AmountAfterDiscount)
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
    // OVERALL SETTLEMENT STATUS
    // =========================================================

    public bool IsSettlementComplete
    {
        get
        {
            if (IsFullyAdjusted)
                return true;

            if (IsReceivable)
            {
                return Math.Abs(
                           ReceivableBalance)
                       <= BalanceTolerance;
            }

            if (IsRefund)
            {
                return Math.Abs(
                           RefundBalance)
                       <= BalanceTolerance;
            }

            return false;
        }
    }


    public bool CanFinalise =>
        IsDiscountValid &&
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
    // DISCOUNT CHANGED
    // =========================================================

    partial void OnDiscountAmountChanged(
        decimal value)
    {
        /*
         * Discount can change the settlement direction/balance.
         *
         * Example:
         *
         * Amount payable : 10,000
         * Discount       :    500
         * Receivable     :  9,500
         */

        Recalculate();
    }


    // =========================================================
    // RECEIPT COMMANDS
    // =========================================================

    [RelayCommand]
    private void AddReceipt()
    {
        var line =
            new InvoiceSettlementLine();

        Receipts.Add(line);

        Recalculate();
    }


    [RelayCommand]
    private void RemoveReceipt(
        InvoiceSettlementLine? line)
    {
        if (line is null)
            return;

        Receipts.Remove(line);

        Recalculate();
    }


    // =========================================================
    // REFUND COMMANDS
    // =========================================================

    [RelayCommand]
    private void AddRefund()
    {
        var line =
            new InvoiceSettlementLine();

        Refunds.Add(line);

        Recalculate();
    }


    [RelayCommand]
    private void RemoveRefund(
        InvoiceSettlementLine? refund)
    {
        if (refund is null)
            return;

        Refunds.Remove(refund);

        Recalculate();
    }


    // =========================================================
    // CREDIT
    // =========================================================

    partial void OnUseCreditChanged(
        bool value)
    {
        if (!value)
        {
            CreditAmount = 0M;
        }
        else if (IsReceivable)
        {
            var remaining =
                ReceivableAmount
                - TotalReceived;

            CreditAmount =
                remaining > 0M
                    ? remaining
                    : 0M;
        }

        Recalculate();
    }


    partial void OnCreditAmountChanged(
        decimal value)
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
         * Settlement direction has changed.
         *
         * Receivable:
         *      Customer -> Shop
         *
         * Refund:
         *      Shop -> Customer
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
        // -----------------------------------------------------
        // Discount
        // -----------------------------------------------------

        if (!IsDiscountValid)
            return false;


        // -----------------------------------------------------
        // Receipts
        // -----------------------------------------------------

        if (Receipts.Any(x =>
                x.Amount <= 0M ||
                string.IsNullOrWhiteSpace(
                    x.PaymentMode)))
        {
            return false;
        }


        // -----------------------------------------------------
        // Refunds
        // -----------------------------------------------------

        if (Refunds.Any(x =>
                x.Amount <= 0M ||
                string.IsNullOrWhiteSpace(
                    x.PaymentMode)))
        {
            return false;
        }


        // -----------------------------------------------------
        // Credit
        // -----------------------------------------------------

        if (CreditAmount < 0M)
            return false;

        return true;
    }


    private void ValidateSettlement()
    {
        ValidationMessage = null;


        // =====================================================
        // DISCOUNT VALIDATION
        // =====================================================

        if (DiscountAmount < 0M)
        {
            ValidationMessage =
                "Discount cannot be negative.";

            return;
        }


        if (NetSettlementAmount > 0M &&
            DiscountAmount >
            NetSettlementAmount)
        {
            ValidationMessage =
                "Discount cannot exceed the amount payable.";

            return;
        }


        // =====================================================
        // FULLY ADJUSTED
        // =====================================================

        if (IsFullyAdjusted)
        {
            /*
             * If Discount itself has reduced the balance to zero,
             * there must not be any additional receipt/credit/refund.
             */

            if (TotalReceived > BalanceTolerance)
            {
                ValidationMessage =
                    "Receipt is not required because the invoice is fully adjusted.";

                return;
            }

            if (EffectiveCreditAmount >
                BalanceTolerance)
            {
                ValidationMessage =
                    "Credit is not required because the invoice is fully adjusted.";

                return;
            }

            if (TotalRefunded >
                BalanceTolerance)
            {
                ValidationMessage =
                    "Refund is not required because the invoice is fully adjusted.";

                return;
            }

            ValidationMessage = null;
            return;
        }


        // =====================================================
        // CUSTOMER OWES SHOP
        // =====================================================

        if (IsReceivable)
        {
            if (Receipts.Any(
                    x => x.Amount <= 0M))
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


            ValidationMessage = null;
            return;
        }


        // =====================================================
        // SHOP OWES CUSTOMER
        // =====================================================

        if (IsRefund)
        {
            if (Refunds.Any(
                    x => x.Amount <= 0M))
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


            ValidationMessage = null;
        }
    }


    // =========================================================
    // RECALCULATION
    // =========================================================

    private void Recalculate()
    {
        // -----------------------------------------------------
        // Discount
        // -----------------------------------------------------

        OnPropertyChanged(
            nameof(AmountAfterDiscount));

        OnPropertyChanged(
            nameof(IsDiscountValid));


        // -----------------------------------------------------
        // Settlement direction
        // -----------------------------------------------------

        OnPropertyChanged(
            nameof(IsReceivable));

        OnPropertyChanged(
            nameof(IsRefund));

        OnPropertyChanged(
            nameof(IsFullyAdjusted));


        // -----------------------------------------------------
        // Receivable
        // -----------------------------------------------------

        OnPropertyChanged(
            nameof(ReceivableAmount));

        OnPropertyChanged(
            nameof(TotalReceived));

        OnPropertyChanged(
            nameof(EffectiveCreditAmount));

        OnPropertyChanged(
            nameof(ReceivableBalance));

        OnPropertyChanged(
            nameof(HasReceivableShortfall));

        OnPropertyChanged(
            nameof(HasReceiptExcess));


        // -----------------------------------------------------
        // Refund
        // -----------------------------------------------------

        OnPropertyChanged(
            nameof(RefundPayable));

        OnPropertyChanged(
            nameof(TotalRefunded));

        OnPropertyChanged(
            nameof(RefundBalance));

        OnPropertyChanged(
            nameof(HasRefundShortfall));

        OnPropertyChanged(
            nameof(HasRefundExcess));


        // -----------------------------------------------------
        // Overall settlement
        // -----------------------------------------------------

        OnPropertyChanged(
            nameof(IsSettlementComplete));

        ValidateSettlement();

        OnPropertyChanged(
            nameof(CanFinalise));
    }
}