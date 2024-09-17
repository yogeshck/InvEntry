using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class BaseController<T> : ControllerBase where T : class
    {

        protected readonly IRepositoryBase<T> _repository;

        public BaseController(IRepositoryBase<T> repository)
        {
            _repository = repository;
        }

        // GET: api/<BaseController>
        [HttpGet]
        public IEnumerable<T> Get()
        {
            return _repository.GetAll();
        }

        // GET api/<BaseController>/5
        public virtual T? GetValue<TProperty>(Expression<Func<T, bool>> predicate)
        {
            return _repository.Get(predicate);
        }

        // POST api/<BaseController>
        [HttpPost]
        public T Post([FromBody] T value)
        {
            _repository.Add(value);
            return value;
        }
    }
}
