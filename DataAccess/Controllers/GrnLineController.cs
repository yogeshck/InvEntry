﻿using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrnLineController : ControllerBase
    {
        private IRepositoryBase<GrnLine> _grnLineRepository;

        public GrnLineController(IRepositoryBase<GrnLine> grnLineRepository)
        {
            _grnLineRepository = grnLineRepository;
        }

        // GET: api/<GrnController>
        [HttpGet]
        public IEnumerable<GrnLine> GetLines()
        {
            return _grnLineRepository.GetAll();
        }

        // GET api/<GrnController>/5
        //[HttpGet("{grnNbr}")]
        //public GrnLine? Get(long grnNbr)
        //{
        //    return _grnLineRepository.Get(x => x.gr == grnNbr);
        //}

        // GET api/<GrnController>/5
        [HttpGet("{grnHdrGkey}")]
        public async Task<IActionResult> GetByHdrGKey(int grnHdrGkey)
        {
            return Ok(_grnLineRepository.GetList(x => x.GrnHdrGkey == grnHdrGkey));
            
        }

        // POST api/<GrnController>
        [HttpPost]
        public GrnLine Post([FromBody] GrnLine value)
        {
            _grnLineRepository.Add(value);
            return value;
        }

        // PUT api/<GrnController>/5
        //[HttpPut("{grnNbr}")]
        //public void Put(int grnNbr, [FromBody] Line value)
        //{
        //    value.InvLineNbr = grnNbr;
        //    _grnLineRepository.Update(value);
        //}

        // DELETE api/<GrnLineController>/5
        //[HttpDelete("{grnNbr}")]
        //public void Delete(long grnNbr)
        //{
        //    _grnLineRepository.Remove(Get(grnNbr));
        //}
    }
}
