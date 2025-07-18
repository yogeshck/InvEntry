﻿using CommunityToolkit.Mvvm.ComponentModel;
using DevExpress.XtraReports.UI;
using InvEntry.Models;
using InvEntry.Reports;
//using mijmsReports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.ViewModels
{
    public partial class ReportDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private XtraReport _report;
        
        private readonly IReportFactoryService _reportFactoryService;

        public ReportDialogViewModel(IReportFactoryService reportFactoryService)
        {
            _reportFactoryService = reportFactoryService;
        }

        public void Init(string pInvoiceNbr)
        {
            Report = _reportFactoryService.CreateInvoiceReport(pInvoiceNbr);
        }

        public void EstInit(string pEstimateNbr, int estGkey, OrgThisCompanyView company)
        {
            Report = _reportFactoryService.CreateEstimateReport(pEstimateNbr, estGkey, company);
        }

        public void DNInit(string pEstimateNbr, int estGkey, OrgThisCompanyView company)
        {
            Report = _reportFactoryService.CreateDeliveryNoteReport(pEstimateNbr, estGkey, company);
        }

        public void FinStmtPCInit(DateTime pFromDate, DateTime pToDate, string statementType)
        {
            Report = _reportFactoryService.CreateFinStatementReport(pFromDate, pToDate, statementType);
        }


    }

}
