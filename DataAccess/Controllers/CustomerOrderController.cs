using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerOrderController : ControllerBase
    {

        private string DocumentPrefixFormat = "B{0}";

        private IRepositoryBase<CustomerOrder> _customerOrderRepository;
        private readonly IRepositoryBase<VoucherType> _voucherTypeRepo;

        public CustomerOrderController( IRepositoryBase<CustomerOrder> customerOrderRepository ,
                                        IRepositoryBase<VoucherType> voucherTypeRepo            ) 
        {
            _customerOrderRepository = customerOrderRepository;
            _voucherTypeRepo = voucherTypeRepo;
        }

        // GET: api/<CustomerOrderController>
        [HttpGet]
        public IEnumerable<CustomerOrder> GetCustomerOrder()
        {
            return _customerOrderRepository.GetAll();
        }

        // GET: api/<CustomerOrderController>/24-Sep-2024/25-Sep-2024
        [HttpPost("filter")]
        public IEnumerable<CustomerOrder> FilterHeader([FromBody] DateSearchOption criteria)
        {
            return _customerOrderRepository.GetList(x => x.OrderDate.HasValue && x.OrderDate.Value.Date >= criteria.From.Date &&
                                                        x.OrderDate.Value.Date <= criteria.To.Date);
        }

        // GET api/<CustomerOrderController>/5
        [HttpGet("{orderNbr}")]
        public CustomerOrder? Get(string orderNbr)
        {
            return _customerOrderRepository.Get(x => x.OrderNbr == orderNbr);
        }

        // POST api/<CustomerOrderController>
        [HttpPost]
        public CustomerOrder Post([FromBody] CustomerOrder value)
        {

            var voucherType = _voucherTypeRepo.Get(x => x.DocumentType == "Sale Invoice"); // value.VoucherType);

            voucherType.LastUsedNumber++;

            _voucherTypeRepo.Update(voucherType);

            DocumentPrefixFormat = voucherType.DocNbrPrefix;

            value.OrderNbr = string.Format("{0}{1}", DocumentPrefixFormat,
                            voucherType?.LastUsedNumber?.ToString($"D{voucherType.DocNbrLength}"));

            _customerOrderRepository.Add(value);
            return value;
        }

        // PUT api/<CustomerOrderController>/5
        [HttpPut("{orderNbr}")]
        public void Put(string orderNbr, [FromBody] CustomerOrder value)
        {
            value.OrderNbr = orderNbr;
            _customerOrderRepository.Update(value);
        }

        // DELETE api/<CustomerOrderController>/5
        [HttpDelete("{orderNbr}")]
        public void Delete(string orderNbr)
        {
            _customerOrderRepository.Remove(Get(orderNbr));
        }
    }

}