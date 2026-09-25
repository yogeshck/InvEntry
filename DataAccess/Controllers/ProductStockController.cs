using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductStockController : ControllerBase
    {
        private readonly IRepositoryBase<ProductStock> _productStock;
        private readonly ILogger<ProductStockController> _logger;
        private readonly MijmsContext _context;

        public ProductStockController( IRepositoryBase<ProductStock> _productStockRepo,
                                        MijmsContext context,
                                        ILogger<ProductStockController> logger) 
        {
            _productStock = _productStockRepo;
            _context = context;
            _logger = logger;
        }

        // GET: api/<ProductStockController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Product Stock");
            return Ok(_productStock.GetAll());
        }

        // GET api/<ProductStockController>/5
        [HttpGet("key/{key}")]
        public async Task<IActionResult> GetByKey(int key)
        {
            //var pstk = _productStock.GetAll().FirstOrDefault(x => x.ProductSku == productSku);

            return Ok(_productStock.Get(x => x.Gkey == key &&
                                            x.IsProductSold == false));

            //Ok(_productStock.Get(x => x.ProductSku == productSku));
        }

        // GET api/<ProductStockController>/5
        [HttpGet("{productSku}")]
        public async Task<IActionResult> Get(string productSku)
        {
            //var pstk = _productStock.GetAll().FirstOrDefault(x => x.ProductSku == productSku);

            return Ok(_productStock.GetAll().FirstOrDefault(x => x.ProductSku == productSku));
                
                //Ok(_productStock.Get(x => x.ProductSku == productSku));
        }

        // GET api/<ProductStockController>/5
        [HttpGet("exact/{productSku}")]
        public IActionResult GetExact(string productSku)
        {
            var stock = _productStock.Get(x => x.ProductSku == productSku);
            return stock is null ? NotFound() : Ok(stock);
        }

        [HttpGet("stock/{productSku}")]
        public IActionResult GetStock(string productSku)
        {

            return Ok(_productStock.GetList(x => x.ProductSku == productSku &&
                                                                x.IsProductSold == false
                                                                ).FirstOrDefault());

        }


        [HttpGet("pending/grn-line-summary/{grnLineSummaryGkey:int}")]
        public IActionResult GetPendingByGrnLineSummary(int grnLineSummaryGkey)
        {
            var records = _productStock.GetList(x =>
                x.GrnLineSummaryGkey == grnLineSummaryGkey &&
                x.Status == "Pending Tag" &&
                x.IsBarcodePrinted == false &&
                x.IsProductSold == false);

            return Ok(records.OrderBy(x => x.Gkey).ToList());
        }

        [HttpPost("{gkey:int}/reserve-sku")]
        public async Task<IActionResult> ReserveSku(int gkey)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            var stock = await _context.ProductStocks
                .FirstOrDefaultAsync(x => x.Gkey == gkey);

            if (stock is null)
                return NotFound($"Product stock {gkey} was not found.");

            if (!string.IsNullOrWhiteSpace(stock.ProductSku) &&
                !stock.ProductSku.StartsWith("TMP-", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.CommitAsync();
                return Ok(stock);
            }

            if (!string.Equals(stock.Status, "Pending Tag", StringComparison.OrdinalIgnoreCase) ||
                stock.IsBarcodePrinted == true || stock.IsProductSold == true)
            {
                return BadRequest("Only pending, unprinted stock can reserve a SKU.");
            }

            if (string.IsNullOrWhiteSpace(stock.Category))
                return BadRequest("Product category is required to reserve a SKU.");

            var reference = await _context.MtblReferences.FirstOrDefaultAsync(x =>
                x.RefName == "PRODUCT_CATEGORY" && x.RefCode == stock.Category);

            if (reference is null || string.IsNullOrWhiteSpace(reference.RefDesc))
                return BadRequest($"SKU sequence is not configured for category '{stock.Category}'.");

            if (!int.TryParse(reference.RefValue, out int sequence))
                return BadRequest($"SKU sequence for category '{stock.Category}' is invalid.");

            var product = stock.ProductGkey.HasValue
                ? await _context.Products.FirstOrDefaultAsync(x => x.Gkey == stock.ProductGkey.Value)
                : null;

            if (product is null)
                return BadRequest("The product required to reserve a SKU was not found.");

            string tagPurityCode = product.Purity switch
            {
                "916" => "2",
                "750" => "8",
                _ => string.Empty
            };

            int reservedSequence = checked(sequence + 1);
            stock.ProductSku = $"{reference.RefDesc}{tagPurityCode}-{reservedSequence:D4}";
            stock.ModifiedOn = DateTime.Now;
            reference.RefValue = reservedSequence.ToString();

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(stock);
        }

        // GET api/<MtblReferenceController>/5
        [HttpGet("category/{category}")]
        public IEnumerable<ProductStock> GetCategory(string category)
        {
            return _productStock.GetList(x => x.Category == category &&
                                          x.IsProductSold == false  );
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ProductStock value)
        {
            if (value is null)
                return BadRequest("ProductStock payload is required.");

            _productStock.Add(value);

            await _context.SaveChangesAsync();

            return Ok(value);
        }

        // PUT api/<ProductStockController>/5
        [HttpPut("{gkey:int}")]
        public async Task<IActionResult> Put(int gkey, [FromBody] ProductStock value)
        {
            if (value is null)
                return BadRequest("ProductStock payload is required.");

            if (gkey != value.Gkey)
                return BadRequest("ProductStock route key does not match the payload key.");

            _productStock.Update(value);
            await _context.SaveChangesAsync();

            return Ok(value);
        }

        // DELETE api/<ProductStockController>/5
        [HttpDelete("{productGkey}")]
        public async Task<IActionResult> Delete(decimal productGkey)
        {
            var product = _productStock.Get(x => x.ProductGkey == productGkey);

            if(product is not null)
                _productStock.Remove(product);

            return Ok();
        }


        // DELETE api/<InvoiceController>/5
        [HttpDelete("{gKey}")]
        public void Delete(int gKey)
        {
            var pStk = _productStock.GetId(gKey);

            _productStock.Remove(pStk);
        }
    }
}
