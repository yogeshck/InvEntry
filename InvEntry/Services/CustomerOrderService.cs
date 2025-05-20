using InvEntry.Models;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface ICustomerOrderService
    {
        Task<CustomerOrder> GetCustomerOrder(string orderNbr);

        Task<CustomerOrder> CreateCustomerOrder(CustomerOrder customerOrder);

        Task UpdateHeader(CustomerOrder customerOrder);

        Task<IEnumerable<CustomerOrder>> GetAll(DateSearchOption options);

        Task CreateCustomerOrderLine(CustomerOrderLine line);

        Task CreateCustomerOrderLine(IEnumerable<CustomerOrderLine> line);
    }

    public class CustomerOrderService : ICustomerOrderService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public CustomerOrderService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<CustomerOrder> GetCustomerOrder(string orderNbr)
        {
            return await _mijmsApiService.Get<CustomerOrder>($"api/customerOrder/{orderNbr}");
        }

        public async Task<CustomerOrder> CreateCustomerOrder(CustomerOrder orderNbr)
        {
            return await _mijmsApiService.Post($"api/customerOrder/", orderNbr);
        }

        public async Task UpdateHeader(CustomerOrder orderNbr)
        {
            await _mijmsApiService.Put($"api/customerOrder/{orderNbr.OrderNbr}", orderNbr);
        }

        public async Task CreateCustomerOrderLine(CustomerOrderLine line)
        {
            await _mijmsApiService.Post($"api/customerOrderLine/", line);
        }

        public async Task CreateCustomerOrderLine(IEnumerable<CustomerOrderLine> lines)
        {
            var list = new List<Task>();

            foreach (var line in lines)
                list.Add(CreateCustomerOrderLine(line));

            await Task.WhenAll(list);
        }

        public async Task<IEnumerable<CustomerOrder>> GetAll(DateSearchOption options)
        {

            return await _mijmsApiService.PostEnumerable<CustomerOrder, DateSearchOption>($"api/customerOrder/filter", options);


        }

    }
}
