using DevExpress.Mvvm;
using InvEntry.Models;
using InvEntry.Tally;
using InvEntry.ViewModels;
using InvEntry.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Extension
{
    public static class ReportExtensions
    {
        public static void PrintPreview(this IDialogService reportDialogService, string invoiceHeader)
        {
            var dialogVM = DISource.Resolve<ReportDialogViewModel>();
            dialogVM.Init(invoiceHeader);

            reportDialogService.ShowDialog(null, "Invoice Preview", $"{nameof(ReportDialogView)}", dialogVM);
        }

        public static void PrintPreviewEstimate(this IDialogService reportDialogService, string estimateHeader,
                                                                    int estGkey, OrgThisCompanyView company)
        {
            var dialogVM = DISource.Resolve<ReportDialogViewModel>();
            dialogVM.EstInit(estimateHeader, estGkey, company);

            reportDialogService.ShowDialog(null, "Estimate Preview", $"{nameof(ReportDialogView)}", dialogVM);
        }

        //Customer Purchase
        public static void PrintPreviewOMPurchase(this IDialogService reportDialogService, string omTransNbr,
                                                            int estGkey, OrgThisCompanyView company)
        {
            var dialogVM = DISource.Resolve<ReportDialogViewModel>();
            dialogVM.OMTransInit(omTransNbr); //, estGkey, company);

            reportDialogService.ShowDialog(null, "Old Metal Purchase Preview", $"{nameof(ReportDialogView)}", dialogVM);
        }

        public static void PrintPreviewEstimate(this IDialogService reportDialogService, string estimateHeader,
                                                                    int estGkey)
        {
            PrintPreviewEstimate(reportDialogService, estimateHeader, estGkey, null);
        }

        //Delivery Note
        public static void PrintPreviewOMPurchase(this IDialogService reportDialogService, string docRefNbr)
                                                            
        {
            PrintPreviewOMPurchase(reportDialogService, docRefNbr);
            var dialogVM = DISource.Resolve<ReportDialogViewModel>();
            dialogVM.OMTransInit(docRefNbr); // estGkey, company);

            reportDialogService.ShowDialog(null, "Delivery Note (DN) Preview", $"{nameof(ReportDialogView)}", dialogVM);
        }
        

        public static void PrintPreviewDeliveryNote(this IDialogService reportDialogService, string estimateHeader,
                                                            int estGkey, OrgThisCompanyView company)
        {
            var dialogVM = DISource.Resolve<ReportDialogViewModel>();
            dialogVM.DNInit(estimateHeader, estGkey, company);

            reportDialogService.ShowDialog(null, "Delivery Note (DN) Preview", $"{nameof(ReportDialogView)}", dialogVM);
        }

        public static void PrintPreviewDeliveryNote(this IDialogService reportDialogService, string estimateHeader,
                                                            int estGkey)
        {
            PrintPreviewDeliveryNote(reportDialogService, estimateHeader, estGkey, null);
        }

        public static void PrintPreviewVoucher(this IDialogService reportDialogService,
                                                    int vchGkey)
        {
            PrintPreviewVoucher(reportDialogService, vchGkey, null);
        }

        public static void PrintPreviewVoucher(this IDialogService reportDialogService,
                                                    int vchGkey, OrgThisCompanyView company)
        {
            var dialogVM = DISource.Resolve<ReportDialogViewModel>();
            dialogVM.VoucherInit(vchGkey, company);

            reportDialogService.ShowDialog(null, "Voucher Preview", $"{nameof(ReportDialogView)}", dialogVM);

        }
    }
}
