﻿using InvEntry.Models;
using InvEntry.Store;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.IoC
{
    public static class FormulaIoC
    {
        public static IServiceCollection ConfigureFormulas(this IServiceCollection services, Action<FormulaStore>? action = null)
        {
            FormulaStore.Instance.ConfigureInvoiceLineFormulas();
            FormulaStore.Instance.ConfigureInvoiceHeaderFormulas();

            if (action is not null)
                action.Invoke(FormulaStore.Instance);
            return services;
        }

        private static void ConfigureInvoiceLineFormulas(this FormulaStore store)
        {
            store.AddFormula<InvoiceLine>(x => x.ProdNetWeight, 
                $"[{nameof(InvoiceLine.ProdGrossWeight)}] - [{nameof(InvoiceLine.ProdStoneWeight)}]");

            store.AddFormula<InvoiceLine>(x => x.VaAmount,
                $"[{nameof(InvoiceLine.ProdNetWeight)}] * [{nameof(InvoiceLine.InvlBilledPrice)}] * [{nameof(InvoiceLine.VaPercent)}]");

            store.AddFormula<InvoiceLine>(x => x.InvlTaxableAmount,
                $"[{nameof(InvoiceLine.ProdNetWeight)}] * [{nameof(InvoiceLine.InvlBilledPrice)}] + [{nameof(InvoiceLine.VaAmount)}]");

            store.AddFormula<InvoiceLine>(x => x.InvlCgstAmount,
                $"[{nameof(InvoiceLine.InvlTaxableAmount)}] * [{nameof(InvoiceLine.InvlCgstPercent)}]");

            store.AddFormula<InvoiceLine>(x => x.InvlSgstAmount,
                $"[{nameof(InvoiceLine.InvlTaxableAmount)}] * [{nameof(InvoiceLine.InvlSgstPercent)}]");

            store.AddFormula<InvoiceLine>(x => x.InvlIgstAmount,
                $"[{nameof(InvoiceLine.InvlTaxableAmount)}] * [{nameof(InvoiceLine.InvlIgstPercent)}]");

            store.AddFormula<InvoiceLine>(x => x.InvlTotal,
                $"[{nameof(InvoiceLine.InvlTaxableAmount)}] + [{nameof(InvoiceLine.InvlCgstAmount)}] + [{nameof(InvoiceLine.InvlSgstAmount)}] + [{nameof(InvoiceLine.InvlIgstAmount)}]");
        }


        private static void ConfigureInvoiceHeaderFormulas(this FormulaStore store)
        {
            store.AddFormula<InvoiceHeader>(x => x.RoundOff,
                $"Round({nameof(InvoiceHeader.InvlTaxTotal)})");

            store.AddFormula<InvoiceHeader>(x => x.GrossRcbAmount,
                $"[{nameof(InvoiceHeader.InvlTaxableAmount)}] + [{nameof(InvoiceHeader.RoundOff)}] - [{nameof(InvoiceHeader.DiscountAmount)}]");

            store.AddFormula<InvoiceHeader>(x => x.AmountPayable,
                $"[{nameof(InvoiceHeader.GrossRcbAmount)}] - [{nameof(InvoiceHeader.OldGoldAmount)}] - [{nameof(InvoiceHeader.OldSilverAmount)}]");

            store.AddFormula<InvoiceHeader>(x => x.InvBalance,
                $"[{nameof(InvoiceHeader.AmountPayable)}] - [{nameof(InvoiceHeader.AdvanceAdj)}] - [{nameof(InvoiceHeader.RdAmountAdj)}] - [{nameof(InvoiceHeader.RecdAmount)}]");
        }
    }
}
