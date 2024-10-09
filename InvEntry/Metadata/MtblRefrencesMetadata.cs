using DevExpress.Mvvm.DataAnnotations;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Metadata;

public class MtblRefrencesMetadata : IMetadataProvider<MtblReference>
{
    public void BuildMetadata(MetadataBuilder<MtblReference> builder)
    {
        builder.Property(x => x.GKey).Hidden();
        builder.Property(x => x.SeqNbr).Hidden();
        builder.Property(x => x.CreatedBy).Hidden();
        builder.Property(x => x.CreatedOn).Hidden();
        builder.Property(x => x.SortSeq).Hidden();
        builder.Property(x => x.IsActive).Hidden();
        builder.Property(x => x.Module).Hidden();
        builder.Property(x => x.RefName).Hidden();
        builder.Property(x => x.ModifiedBy).Hidden();
        builder.Property(x => x.ModifiedOn).Hidden();
        builder.Property(x => x.RefDesc).Hidden();
    }
}
