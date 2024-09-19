using DevExpress.Mvvm.DataAnnotations;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Metadata
{
    public class ProductCategoryMetadata : IMetadataProvider<ProductCategory>
    {
        public void BuildMetadata(MetadataBuilder<ProductCategory> builder)
        {
            builder.Property(x => x.GKey).Hidden();
            builder.Property(x => x.CreatedOn).Hidden();
            builder.Property(x => x.CreatedBy).Hidden();
            builder.Property(x => x.ModifiedBy).Hidden();
            builder.Property(x => x.ModifiedBy).Hidden();
            builder.Property(x => x.Sn).Hidden();
            builder.Property(x => x.Name).DisplayName("Category");
        }
    }
}
