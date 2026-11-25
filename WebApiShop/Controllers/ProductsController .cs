using Microsoft.AspNetCore.Mvc;
using Services;
using DTOs;
using Microsoft.AspNetCore.Authorization;
using WebApiShop.Auth;

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

        // GET /api/Products  — returns all products (no filters)
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> Get()
        {
            IEnumerable<ProductDTO> products = await _IProductsServices.GetProducts();
            if (products != null && products.Any())
                return Ok(products);
            return NoContent();
        }

        // GET /api/Products/Filter  — returns paginated + filtered products
        [HttpGet("Filter")]
        [AllowAnonymous]
        public async Task<ActionResult<PageResponseDTO<ProductDTO>>> GetFiltered(
    int position = 1,      // ← default to page 1
    int skip = 10,         // ← default to 10 items per page
    [FromQuery] int?[] categoryIds = null,
    string? description = null,
    int? maxPrice = null,
    int? minPrice = null)
        {
            PageResponseDTO<ProductDTO> pageResponse = await _IProductsServices.GetProducts(
                position, skip, categoryIds, description, maxPrice, minPrice);

            if (pageResponse.Data != null && pageResponse.Data.Count > 0)
                return Ok(pageResponse);
            return NoContent();
        }

        // POST /api/Products
        [HttpPost]
        [AuthorizeRoles(Roles.Admin)]
        public async Task<ActionResult<ProductDTO>> Post([FromBody] ProductDTO product)
        {
            var created = await _IProductsServices.AddProduct(product);
            if (created == null) return BadRequest();
            return CreatedAtAction(nameof(Get), created);
        }

        // PUT /api/Products/{id}
        [HttpPut("{id}")]
        [AuthorizeRoles(Roles.Admin)]
        public async Task<IActionResult> Put(int id, [FromBody] ProductDTO product)
        {
            bool success = await _IProductsServices.UpdateProduct(id, product);
            if (!success) return NotFound();
            return NoContent();
        }

        // DELETE /api/Products/{id}
        [HttpDelete("{id}")]
        [AuthorizeRoles(Roles.Admin)]
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