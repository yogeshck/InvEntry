﻿using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LedgersTransactionController : ControllerBase
    {

        private readonly IRepositoryBase<LedgersTransaction> _ledgersTransactionRepository;

        public LedgersTransactionController(IRepositoryBase<LedgersTransaction> ledgersTransactionRepository)
        {
            _ledgersTransactionRepository = ledgersTransactionRepository;

        }

        // GET: api/<MtblLedgerController>
        [HttpGet]
        public IEnumerable<LedgersTransaction> Get()
        {
            return _ledgersTransactionRepository.GetAll();
        }

        // POST api/<MtblLedgerController>
        [HttpPost]
        public IActionResult Post([FromBody] LedgersTransaction value)
        {
            _ledgersTransactionRepository.Add(value);
            return Ok(value);
        }

        // PUT api/<MtblLedgerController>/5
        [HttpPut]
        public IActionResult Put([FromBody] LedgersTransaction value)
        {
            _ledgersTransactionRepository.Update(value);
            return Ok(value);
        }
    }
}
