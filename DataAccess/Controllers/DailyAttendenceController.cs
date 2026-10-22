using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DailyAttendenceController : ControllerBase
    {

        private readonly IRepositoryBase<DailyAttendence> _dailyAttendence;
        private readonly IUnitOfWork _unitOfWork;

        public DailyAttendenceController(IRepositoryBase<DailyAttendence> dailyAttendenceRepo,
                                         IUnitOfWork unitOfWork)
        {
            _dailyAttendence = dailyAttendenceRepo;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(_dailyAttendence.GetAll());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            return Ok(_dailyAttendence.Get(x => x.Gkey == id));
        }

        // GET api/<ProductStockController>/5
/*        [HttpGet("{mobile}")]
        public async Task<IActionResult> Get(string mobile)
        {
            return Ok(_customer.Get(x => x.MobileNbr == mobile));
        }*/

        // POST api/<ProductStockController>
        [HttpPost]
        public async Task<DailyAttendence> Post([FromBody] DailyAttendence value)
        {
            var attendence = _dailyAttendence.Get(x => x.Gkey == value.Gkey);

            if (attendence == null)
            {
                _dailyAttendence.Add(value);
                await _unitOfWork.SaveChangesAsync();
                attendence = _dailyAttendence.Get(x => x.Gkey == value.Gkey);
            }
            else
            {
                attendence.WorkSite = value.WorkSite;
                attendence.WorkStatus = value.WorkStatus;
                attendence.WorkHoursDay = value.WorkHoursDay;
                attendence.FirstIn = value.FirstIn;
                attendence.LastOut = value.LastOut;
                attendence.PunchCount = value.PunchCount;
                attendence.LeaveRequestGkey = value.LeaveRequestGkey;
                attendence.Notes = value.Notes;

                _dailyAttendence.Update(attendence);
                await _unitOfWork.SaveChangesAsync();
            }

            return attendence ?? value;
        }

        [HttpPut("{id}")]
        public async Task Put(int id, [FromBody] DailyAttendence value)
        {
             _dailyAttendence.Update(value);
             await _unitOfWork.SaveChangesAsync();
        }

    }
}
