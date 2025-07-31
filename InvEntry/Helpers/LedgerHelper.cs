using CommunityToolkit.Mvvm.ComponentModel;
using DevExpress.Charts.Designer.Native;
using DevExpress.Mvvm;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Helper;


public partial class LedgerHelper : ObservableObject
{
    private LedgersHeader ledgerHeader;

    private MtblLedger mtblFromLedger;

    private MtblLedger mtblToLedger;

    private LedgersTransactions _ledgersTransactions;

    private readonly ILedgerService _ledgerService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IMtblLedgersService _mtblLedgersService;

    public LedgerHelper(ILedgerService ledgerService,
                        IMessageBoxService messageBoxService,
                        IMtblLedgersService mtblLedgersService)
    {
        _ledgerService = ledgerService;
        _messageBoxService = messageBoxService;
        _mtblLedgersService = mtblLedgersService;

        ledgerHeader = new();
        // SetHeader();

    }

    private async Task<LedgersHeader> GetLedgerBalanceAsync(int ledgerGkey, int custGkey)
    {

        var ledgerHdr = await _ledgerService.GetHeader(ledgerGkey, custGkey); // MtblLedger.GKey, Buyer.GKey);   //hard coded to be fixed

        if (ledgerHdr is null)
        {

            return ledgerHdr;    //at present return null
        }

        return ledgerHdr;
    }

    private async Task<MtblLedger> GetFromLedgerKey(string transType)
    {
        var accountCode = 3000;    // recurring deposit -- need to workout and remove hard coded values
        if (transType == "Advance Receipt")
            accountCode = 1000;

        var mtblLedger = await _mtblLedgersService.GetLedger(accountCode);   //1000 - for AdvanceReceipt pass account code
        return mtblLedger;
    }

    public async Task ProcessInvoiceAdvanceAsync(InvoiceHeader invheader)
    {

        if (invheader.AdvanceAdj > 0)
            await ProcessSettlements(invheader, "Advance Receipt");

        //SetHeader();

      //  if (invheader.RdAmountAdj > 0)
      //      await ProcessSettlements(invheader, "Recurring Deposit");

    }

    private async Task ProcessSettlements(InvoiceHeader invheader, string transType) {

        LedgersTransactions ledgerTrans = new();

        mtblFromLedger = await GetFromLedgerKey(transType);

        //get customer advanace balance
        var ledgerBalance = await GetLedgerBalanceAsync(mtblFromLedger.GKey, invheader.CustGkey.GetValueOrDefault());

        //check customer has already ledger entry
        //LedgerHeader = await _ledgerService.GetHeader(100, 1000); // MtblLedger.GKey, Buyer.GKey);   //hard coded to be fixed

        if (ledgerBalance is not null)
        {

            /*        if ( (ledgerBalance.CurrentBalance < 1) || 
                            (ledgerBalance.CurrentBalance < invheader.AdvanceAdj.GetValueOrDefault() ) ||
                            (ledgerBalance.CurrentBalance < invheader.RdAmountAdj.GetValueOrDefault() )       )
                    {
                        _messageBoxService.ShowMessage($"Available Balance is Rs.  {ledgerBalance.CurrentBalance} only...",
                                    "Insufficient Balance", MessageButton.OK, MessageIcon.Error);
                        //return;
                    }*/

            if (transType == "Advance Receipt")
            {
                ledgerHeader.CurrentBalance = ledgerBalance.CurrentBalance.GetValueOrDefault() -
                                                        invheader.AdvanceAdj.GetValueOrDefault();
            }
            else if (transType == "Recurring Deposit")
            {
                ledgerHeader.CurrentBalance = ledgerBalance.CurrentBalance.GetValueOrDefault() -
                                                        invheader.RdAmountAdj.GetValueOrDefault();
            }

            if (ledgerHeader.CurrentBalance < 0)
                ledgerHeader.CurrentBalance = 0;

            SetLedgerTransaction(ledgerTrans, invheader);

           await _ledgerService.UpdateHeader(ledgerHeader);

            ledgerHeader.Transactions.Add(ledgerTrans);

            await _ledgerService.CreateLedgersTransactions(ledgerHeader.Transactions);



            //await _ledgerService.UpdateHeader(LedgerHeader);    -- need to work out tomorrow - mouli
        }
        else
        {

            // LedgersHeader ledgrHeader = new();

            ledgerHeader.MtblLedgersGkey = mtblFromLedger.GKey; // MtblLedger.GKey;
            ledgerHeader.CustGkey = invheader.CustGkey;
            ledgerHeader.BalanceAsOn = DateTime.Now;

            ledgerHeader.CurrentBalance = 0; // Header.AdvanceAdj.GetValueOrDefault();

            ledgerHeader = await _ledgerService.CreateHeader(ledgerHeader);

            //LedgersTransactions ledgerTrans = new();

            SetLedgerTransaction(ledgerTrans, invheader);

            ledgerHeader.Transactions.Add(ledgerTrans);

            await _ledgerService.CreateLedgersTransactions(ledgerHeader.Transactions);


        }

    }

    private void SetLedgerTransaction(LedgersTransactions ledgerTrans, InvoiceHeader invheader)
    {
        ledgerTrans.DrCr = "Cr";
        ledgerTrans.TransactionAmount = invheader.AdvanceAdj.GetValueOrDefault();
        ledgerTrans.DocumentNbr = invheader.InvNbr;
        ledgerTrans.DocumentDate = invheader.InvDate;
        ledgerTrans.LedgerHdrGkey = ledgerHeader.GKey;
        ledgerTrans.TransactionDate = DateTime.Now;
        ledgerTrans.Status = true;

    }

}
