﻿using DevExpress.Mvvm;
using InvEntry.Models;
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

        public static void PrintPreviewEstimate(this IDialogService reportDialogService, string estimateHeader, OrgThisCompanyView company)
        {
            var dialogVM = DISource.Resolve<ReportDialogViewModel>();
            dialogVM.EstInit(estimateHeader, company);

            reportDialogService.ShowDialog(null, "Estimate Preview", $"{nameof(ReportDialogView)}", dialogVM);
        }
    }
}
