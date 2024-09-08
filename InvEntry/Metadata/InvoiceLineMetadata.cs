using DevExpress.Mvvm.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tern.MI.InvEntry.Models;

namespace InvEntry.Metadata
{
    public class InvoiceLineMetadata : IMetadataProvider<InvoiceLine>
    {
        public void BuildMetadata(MetadataBuilder<InvoiceLine> builder)
        {
            builder.Property(x => x.InvHeader).Hidden();
            builder.Property(x => x.InvHeaderId).Hidden();
        }
    }
}
