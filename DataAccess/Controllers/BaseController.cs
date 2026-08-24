using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
        [Route("api/[controller]")]
        [ApiController]
        public abstract class BaseController<T> : ControllerBase
            where T : class
        {
            protected readonly IRepositoryBase<T> _repository;
            protected readonly IUnitOfWork _unitOfWork;

            protected BaseController(
                IRepositoryBase<T> repository,
                IUnitOfWork unitOfWork)
            {
                _repository = repository;
                _unitOfWork = unitOfWork;
            }


            // =========================================================
            // GET: api/<controller>
            // =========================================================

            [HttpGet]
            public virtual IEnumerable<T> Get()
            {
                return _repository.GetAll();
            }


            // =========================================================
            // Generic lookup helper
            // =========================================================

            public virtual T? GetValue<TProperty>(
                Expression<Func<T, bool>> predicate)
            {
                return _repository.Get(predicate);
            }


            // =========================================================
            // POST: api/<controller>
            // =========================================================

            [HttpPost]
            public virtual async Task<ActionResult<T>> Post(
                [FromBody] T value)
            {
                if (value is null)
                    return BadRequest();

                _repository.Add(value);

                await _unitOfWork.SaveChangesAsync();

                return Ok(value);
            }
        }
    

}
