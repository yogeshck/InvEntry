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
        public Product GetProduct(string productGkey);
    }

    public class ProductService : IProductService
    {
        public Product GetProduct(string productGkey)
        {
            return new Product()
            {
                ProductId = productGkey,
                GrossWeight = Random.Shared.NextInt64(100000, 10000000),
                NetWeight = Random.Shared.NextInt64(100, 999)
            };
        }
    }
}
