using DevExpress.DataAccess.ObjectBinding;
using DevExpress.Utils.StructuredStorage.Internal.Reader;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Models.Report;
using InvEntry.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Reports;

[HighlightedClass]
public class InvoiceDataSource
{

    public InvoiceHeader Header { get; set; }
    public string InvoiceNbr { get; set; }
    private readonly IInvoiceService _invoiceService;
    public List<InvoiceReportModel> _invoiceReports;

    [HighlightedMember]
    public InvoiceDataSource(InvoiceHeader invoiceHeader)
    {

        Header = invoiceHeader;
    }

    [HighlightedMember]
    public InvoiceDataSource(string invoiceNbr)
    {
        InvoiceNbr = invoiceNbr;
        _invoiceService = DISource.Resolve<IInvoiceService>();
        SetHeader();
       
    }

    private async void SetHeader()
    {
        Header = await _invoiceService.GetHeader(InvoiceNbr);
    }


    [HighlightedMember]
    public IEnumerable<InvoiceReportModel> GetInvoiceLines()
    {
        // return Header.Lines;
        return _invoiceReports;
    }

   
}
