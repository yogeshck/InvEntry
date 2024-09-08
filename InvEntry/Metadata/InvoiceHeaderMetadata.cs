using DevExpress.Mvvm.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tern.MI.InvEntry.Models;

namespace InvEntry.Metadata;

public class InvoiceHeaderMetadata : IMetadataProvider<InvoiceHeader>
{
    public void BuildMetadata(MetadataBuilder<InvoiceHeader> builder)
    {
        builder.DataFormLayout()
            .GroupBox("Header")
                .ContainsProperty(x => x.InvCustMobile)
                .ContainsProperty(x => x.InvNbr)
                .ContainsProperty(x => x.InvDate)
            .EndGroup();

        builder.Property(x => x.InvDate).DateTimeDataType();
        builder.Property(x => x.InvCustMobile).PhoneNumberDataType();
    }
}
