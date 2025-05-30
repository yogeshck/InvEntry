﻿using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class InvoiceArReceipt
{
    public int Gkey { get; set; }

    public int? CustGkey { get; set; }

    public int? InvoiceGkey { get; set; }

    public string? InvoiceNbr { get; set; }

    public decimal? InvoiceReceivableAmount { get; set; }

    public decimal? BalanceAfterAdj { get; set; }

    public int SeqNbr { get; set; }

    public string? TransactionType { get; set; }

    public string? ModeOfReceipt { get; set; }

    public decimal? BalBeforeAdj { get; set; }

    public decimal? AdjustedAmount { get; set; }

    public string? InternalVoucherNbr { get; set; }

    public DateTime? InternalVoucherDate { get; set; }

    public string? ExternalTransactionId { get; set; }

    public DateTime? ExternalTransactionDate { get; set; }

    public string? BankName { get; set; }

    public string? OtherReference { get; set; }

    public string? SenderBankAccountNbr { get; set; }

    public int? SenderBankGkey { get; set; }

    public string? SenderBankBranch { get; set; }

    public string? SenderBankIfscCode { get; set; }

    public string? CompanyBankAccountNbr { get; set; }

    public string? Status { get; set; }

    public string? InvoiceReceiptNbr { get; set; }
}
