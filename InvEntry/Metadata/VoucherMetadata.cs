using DevExpress.Mvvm.DataAnnotations;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Metadata;

public class VoucherMetadata : IMetadataProvider<Voucher>
{
    public void BuildMetadata(MetadataBuilder<Voucher> builder)
    {
        builder.Property(x => x.GKey).Hidden();
    }
}
