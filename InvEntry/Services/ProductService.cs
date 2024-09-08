using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IProductService
    {
        public Product GetProduct(long productGkey);
    }

    public class ProductService : IProductService
    {
        public Product GetProduct(long productGkey)
        {
            return new Product()
            {
                ProductGkey = productGkey,
                GrossAmount = Random.Shared.NextInt64(100000, 10000000),
                ProductName = $"Product{Random.Shared.NextInt64(100, 999)}"
            };
        }
    }
}
