using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {

        private readonly IRepositoryBase<Employee> _employee;

        public EmployeeController(IRepositoryBase<Employee> employeeRepo)
        {
            _employee = employeeRepo;
        }

        // GET: api/<ProductStockController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(_employee.GetAll());
        }

        // GET api/<EmployeeController>/5
        [HttpGet("{mobile}")]
        public async Task<IActionResult> Get(string mobile)
        {
            return Ok(_employee.Get(x => x.MobileNbr == mobile));
        }

        // POST api/<ProductStockController>
        [HttpPost]
        public Employee Post([FromBody] Employee value)
        {
            var employee = _employee.Get(x => x.MobileNbr == value.MobileNbr);

            if (employee == null)
            {
                _employee.Add(value);
                employee = _employee.Get(x => x.MobileNbr == value.MobileNbr);
            }
            else
            {
                employee.FirstName = value.FirstName;
                employee.LastName = value.LastName;
                _employee.Update(employee);
            }

            return employee ?? value;
        }

        // PUT api/<ProductStockController>/5
        [HttpPut("{MobileNbr}")]
        public void Put(string MobileNbr, [FromBody] Employee value)
        {
            _employee.Update(value);
        }

    }
}
