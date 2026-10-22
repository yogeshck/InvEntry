using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerOrderLineController : ControllerBase
    {

        private IRepositoryBase<CustomerOrderLine> _customerOrderLineRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CustomerOrderLineController(IRepositoryBase<CustomerOrderLine> customerOrderLineRepository,
                                           IUnitOfWork unitOfWork)
        {
            _customerOrderLineRepository = customerOrderLineRepository;
            _unitOfWork = unitOfWork;
        }

        // GET: api/<CustomerOrderLineController>
        [HttpGet]
        public IEnumerable<CustomerOrderLine> GetAllLines()
        {
            return _customerOrderLineRepository.GetAll();
        }

        // GET api/<CustomerOrderLineController>/5
        [HttpGet("{orderNbr}")]
        public IEnumerable<CustomerOrderLine?> GetLinesByOrderNbr(string orderNbr)
        {
            return _customerOrderLineRepository.GetList(x => x.OrderNbr == orderNbr);
        }


        // POST api/<CustomerOrderLineController>
        [HttpPost]
        public async Task<CustomerOrderLine> Post([FromBody] CustomerOrderLine value)
        {
            _customerOrderLineRepository.Add(value);
            await _unitOfWork.SaveChangesAsync();
            return value;
        }

        // PUT api/<CustomerOrderLineController>/5
        [HttpPut("{orderLineNbr}")]
        public async Task Put(int orderLineNbr, [FromBody] CustomerOrderLine value)
        {
            value.OrderLineNbr = orderLineNbr;
            _customerOrderLineRepository.Update(value);
            await _unitOfWork.SaveChangesAsync();
        }



    }

}