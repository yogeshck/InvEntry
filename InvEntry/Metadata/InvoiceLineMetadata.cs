using DevExpress.Mvvm.DataAnnotations;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Metadata
{
    public class InvoiceLineMetadata : IMetadataProvider<InvoiceLine>
    {
        public void BuildMetadata(MetadataBuilder<InvoiceLine> builder)
        {
            builder.Property(x => x.GKey).Hidden();
            builder.Property(x => x.InvoiceId).Hidden();
        }
    }
}
