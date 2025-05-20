using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerOrderLineController : ControllerBase
    {

        private IRepositoryBase<CustomerOrderLine> _customerOrderLineRepository;

        public CustomerOrderLineController(IRepositoryBase<CustomerOrderLine> customerOrderLineRepository)
        {
            _customerOrderLineRepository = customerOrderLineRepository;
        }

        // GET: api/<CustomerOrderLineController>
        [HttpGet]
        public IEnumerable<CustomerOrderLine> GetHeader()
        {
            return _customerOrderLineRepository.GetAll();
        }

        // GET api/<CustomerOrderLineController>/5
        [HttpGet("{orderNbr}")]
        public CustomerOrderLine? Get(long orderNbr)
        {
            return _customerOrderLineRepository.Get(x => x.OrderLineNbr == orderNbr);
        }

        // POST api/<CustomerOrderLineController>
        [HttpPost]
        public CustomerOrderLine Post([FromBody] CustomerOrderLine value)
        {
            _customerOrderLineRepository.Add(value);
            return value;
        }

        // PUT api/<CustomerOrderLineController>/5
        [HttpPut("{orderLineNbr}")]
        public void Put(int orderLineNbr, [FromBody] CustomerOrderLine value)
        {
            value.OrderLineNbr = orderLineNbr;
            _customerOrderLineRepository.Update(value);
        }

        // DELETE api/<CustomerOrderLineController>/5
        [HttpDelete("{orderLineNbr}")]
        public void Delete(long orderLineNbr)
        {
            _customerOrderLineRepository.Remove(Get(orderLineNbr));
        }

    }

}