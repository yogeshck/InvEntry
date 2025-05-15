using InvEntry.Models;
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
            FormulaStore.Instance.ConfigureEstimateLineFormulas();
            FormulaStore.Instance.ConfigureGRNLineSumryFormulas();
            FormulaStore.Instance.ConfigureGRNLineFormulas();

            if (action is not null)
                action.Invoke(FormulaStore.Instance);
            return services;
        }

        private static void ConfigureInvoiceLineFormulas(this FormulaStore store)
        {
            store.AddFormula<InvoiceLine>(x => x.ProdNetWeight, 
                $"[{nameof(InvoiceLine.ProdGrossWeight)}] - [{nameof(InvoiceLine.ProdStoneWeight)}]", precision: 3);

            store.AddFormula<InvoiceLine>(x => x.InvlGrossAmt,
                $"[{nameof(InvoiceLine.ProdNetWeight)}] * [{nameof(InvoiceLine.InvlBilledPrice)}]");

            store.AddFormula<InvoiceLine>(x => x.VaAmount,
                $"[{nameof(InvoiceLine.InvlGrossAmt)}] * Round(([{nameof(InvoiceLine.VaPercent)}] / 100), 3)");

            store.AddFormula<InvoiceLine>(x => x.InvlTaxableAmount,
                $"[{nameof(InvoiceLine.InvlGrossAmt)}] + [{nameof(InvoiceLine.VaAmount)}] + [{nameof(InvoiceLine.InvlStoneAmount)}]");

            store.AddFormula<InvoiceLine>(x => x.InvlCgstAmount,
                $"[{nameof(InvoiceLine.InvlTaxableAmount)}] * Round(([{nameof(InvoiceLine.InvlCgstPercent)}]/ 100), 3)");

            store.AddFormula<InvoiceLine>(x => x.InvlSgstAmount,
                $"[{nameof(InvoiceLine.InvlTaxableAmount)}] * Round(([{nameof(InvoiceLine.InvlSgstPercent)}]/ 100), 3)");

            store.AddFormula<InvoiceLine>(x => x.InvlIgstAmount,
                $"[{nameof(InvoiceLine.InvlTaxableAmount)}] * Round(([{nameof(InvoiceLine.InvlIgstPercent)}]/ 100), 3)");

            store.AddFormula<InvoiceLine>(x => x.InvlTotal,
                //$"[{nameof(InvoiceLine.InvlTaxableAmount)}] + [{nameof(InvoiceLine.InvlCgstAmount)}] + [{nameof(InvoiceLine.InvlSgstAmount)}] + [{nameof(InvoiceLine.InvlIgstAmount)}]");
                $"[{nameof(InvoiceLine.InvlTaxableAmount)}]");
        }

        private static void ConfigureEstimateLineFormulas(this FormulaStore store)
        {
            store.AddFormula<EstimateLine>(x => x.ProdNetWeight,
                $"[{nameof(EstimateLine.ProdGrossWeight)}] - [{nameof(EstimateLine.ProdStoneWeight)}]", precision: 3);

            store.AddFormula<EstimateLine>(x => x.EstlGrossAmt,
                $"[{nameof(EstimateLine.ProdNetWeight)}] * [{nameof(EstimateLine.EstlBilledPrice)}]");

            store.AddFormula<EstimateLine>(x => x.VaAmount,
                $"[{nameof(EstimateLine.EstlGrossAmt)}] * Round(([{nameof(EstimateLine.VaPercent)}] / 100), 3)");

            store.AddFormula<EstimateLine>(x => x.EstlTaxableAmount,
                $"[{nameof(EstimateLine.EstlGrossAmt)}] + [{nameof(EstimateLine.VaAmount)}] + [{nameof(EstimateLine.EstlStoneAmount)}]");

            store.AddFormula<EstimateLine>(x => x.EstlCgstAmount,
                $"[{nameof(EstimateLine.EstlTaxableAmount)}] * Round(([{nameof(EstimateLine.EstlCgstPercent)}]/ 100), 3)");

            store.AddFormula<EstimateLine>(x => x.EstlSgstAmount,
                $"[{nameof(EstimateLine.EstlTaxableAmount)}] * Round(([{nameof(EstimateLine.EstlSgstPercent)}]/ 100), 3)");

            store.AddFormula<EstimateLine>(x => x.EstlIgstAmount,
                $"[{nameof(EstimateLine.EstlTaxableAmount)}] * Round(([{nameof(EstimateLine.EstlIgstPercent)}]/ 100), 3)");

            store.AddFormula<EstimateLine>(x => x.EstlTotal,
                //$"[{nameof(EstimateLine.EstlTaxableAmount)}] + [{nameof(EstimateLine.EstlCgstAmount)}] + [{nameof(EstimateLine.EstlSgstAmount)}] + [{nameof(EstimateLine.EstlIgstAmount)}]");
                $"[{nameof(EstimateLine.EstlTaxableAmount)}]");
        }

        private static void ConfigureInvoiceHeaderFormulas(this FormulaStore store)
        {
            store.AddFormula<InvoiceHeader>(x => x.RoundOff,
                $" Round([{nameof(InvoiceHeader.InvlTaxTotal)}]) - [{nameof(InvoiceHeader.InvlTaxTotal)}]");

            store.AddFormula<InvoiceHeader>(x => x.GrossRcbAmount,
                $"[{nameof(InvoiceHeader.InvlTaxTotal)}] + [{nameof(InvoiceHeader.RoundOff)}] - [{nameof(InvoiceHeader.DiscountAmount)}]");

            store.AddFormula<InvoiceHeader>(x => x.AmountPayable,
                $"[{nameof(InvoiceHeader.GrossRcbAmount)}] - [{nameof(InvoiceHeader.OldGoldAmount)}] - [{nameof(InvoiceHeader.OldSilverAmount)}]");

            store.AddFormula<InvoiceHeader>(x => x.InvBalance,
                $"[{nameof(InvoiceHeader.AmountPayable)}] - [{nameof(InvoiceHeader.AdvanceAdj)}] - [{nameof(InvoiceHeader.RdAmountAdj)}] - [{nameof(InvoiceHeader.RecdAmount)}]");
        }

        private static void ConfigureGRNLineSumryFormulas(this FormulaStore store)
        {
            // net weight = gross weight - stone weight 
            // [NW]=[GW]-[SW] -> DevExpress Formula
            // String interpolation - $"{5-2}"
            // nameof()

            store.AddFormula<GrnLineSummary>(x => x.NetWeight,
                $" [{nameof(GrnLineSummary.GrossWeight)}] - [{nameof(GrnLineSummary.StoneWeight)}]",precision:3);
        }

        private static void ConfigureGRNLineFormulas(this FormulaStore store)
        {
            // net weight = gross weight - stone weight 
            // [NW]=[GW]-[SW] -> DevExpress Formula
            // String interpolation - $"{5-2}"
            // nameof()

            store.AddFormula<GrnLine>(x => x.NetWeight,
                $" [{nameof(GrnLine.GrossWeight)}] - [{nameof(GrnLine.StoneWeight)}]",precision:3);

            //rej qty = recd qty - accp qty
            store.AddFormula<GrnLine>(x => x.OrderedQty,
                $" [{nameof(GrnLine.SuppliedQty)}] - 0 ");

            //store.AddFormula<GrnLine>(x => x.ReceivedQty,
            //    $" [{nameof(GrnLine.SuppliedQty)}] - 0 ");

            //store.AddFormula<GrnLine>(x => x.AcceptedQty,
            //    $" [{nameof(GrnLine.SuppliedQty)}] - 0 ");

            store.AddFormula<GrnLine>(x => x.RejectedQty,
                $" [{nameof(GrnLine.ReceivedQty)}] - [{nameof(GrnLine.AcceptedQty)}]");

        }

    }
}
