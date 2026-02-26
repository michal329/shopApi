using Microsoft.AspNetCore.Mvc;
using Services;
using DTOs;

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductsServices _IProductsServices;

        public ProductsController(IProductsServices productsServices)
        {
            _IProductsServices = productsServices;
        }

        // GET /api/Products
        [HttpGet]
        public async Task<ActionResult<PageResponseDTO<ProductDTO>>> Get(
            int position, int skip,
            [FromQuery] int?[] categoryIds,
            string? description,
            int? maxPrice,
            int? minPrice)
        {
            PageResponseDTO<ProductDTO> pageResponse = await _IProductsServices.GetProducts(
                position, skip, categoryIds, description, maxPrice, minPrice);

            if (pageResponse.Data.Count() > 0)
                return Ok(pageResponse);
            return NoContent();
        }

        // POST /api/Products
        [HttpPost]
        public async Task<ActionResult<ProductDTO>> Post([FromBody] ProductDTO product)
        {
            var created = await _IProductsServices.AddProduct(product);
            if (created == null) return BadRequest();
            return CreatedAtAction(nameof(Get), created);
        }

        // PUT /api/Products/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] ProductDTO product)
        {
            bool success = await _IProductsServices.UpdateProduct(id, product);
            if (!success) return NotFound();
            return NoContent();
        }

        // DELETE /api/Products/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                bool success = await _IProductsServices.DeleteProduct(id);
                if (!success) return NotFound();
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}