using DevExpress.Mvvm.DataAnnotations;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Metadata;

public class InvoiceHeaderMetadata : IMetadataProvider<InvoiceHeader>
{
    public void BuildMetadata(MetadataBuilder<InvoiceHeader> builder)
    {
        builder.DataFormLayout()
            .GroupBox("Header")
                .ContainsProperty(x => x.CustMobile)
                .ContainsProperty(x => x.InvNbr)
                .ContainsProperty(x => x.InvDate)
            .EndGroup();

        builder.Property(x => x.InvDate).DateTimeDataType();
        builder.Property(x => x.CustMobile).PhoneNumberDataType();
    }
}
