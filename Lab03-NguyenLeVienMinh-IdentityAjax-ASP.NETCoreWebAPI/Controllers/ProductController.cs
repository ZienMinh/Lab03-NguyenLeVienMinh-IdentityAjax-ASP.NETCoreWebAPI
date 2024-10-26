using BusinessObjects.Models.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nest;
using Services.Interfaces;

namespace Lab03_NguyenLeVienMinh_IdentityAjax_ASP.NETCoreWebAPI.Controllers
{
	[ApiController]
	[Route("api/products")]
	public class ProductController : ControllerBase
	{
		private readonly IProductService _productService;
		private readonly IElasticClient _elasticClient;

		public ProductController(IProductService productService, IElasticClient elasticClient)
		{
			_productService = productService;
			_elasticClient = elasticClient;
		}

		// GET: api/products/{id}
		[HttpGet("{id}")]
		[Authorize(Roles = "ADMIN")]
		public async Task<IActionResult> GetProduct(int id)
		{
			var response = await _productService.FindProductById(id);
			if (response.IsSucceed)
			{
				return Ok(response.Result);
			}
			return NotFound(response.Message); // 404 Not Found if product doesn't exist
		}

		// POST: api/products
		[HttpPost]
		[Authorize(Roles = "ADMIN")]
		public async Task<IActionResult> CreateProduct([FromBody] ProductRequestModel product)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState); // 400 Bad Request if product data is invalid
			}

			var response = await _productService.CreateProduct(product);
			if (response.IsSucceed)
			{
				return CreatedAtAction(nameof(GetProduct), new { id = response.Result.ProductId }, response.Result);  // 201 Created
			}
			return BadRequest(response.Message);
		}

		// PUT: api/products/{id}
		[HttpPut("{id}")]
		[Authorize(Roles = "ADMIN")]
		public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductRequestModel productRequest)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState); // 400 Bad Request if product data is invalid
			}

			var response = await _productService.UpdateProduct(id, productRequest);

			if (response.IsSucceed)
			{
				return Ok(response.Result); // 200 OK if product update is successful
			}
			return BadRequest(response.Message); // 400 Bad Request with error message
		}

		// DELETE: api/products/{id}
		[HttpDelete("{id}")]
		[Authorize(Roles = "ADMIN")]
		public async Task<IActionResult> DeleteProduct(int id)
		{
			var existingProduct = await _productService.FindProductById(id);
			if (!existingProduct.IsSucceed)
			{
				return NotFound(existingProduct.Message); // 404 Not Found if product doesn't exist
			}

			var response = await _productService.DeleteProduct(existingProduct.Result);
			if (response.IsSucceed)
			{
				return NoContent(); // 204 No Content for successful deletion
			}
			return BadRequest(response.Message); // 400 Bad Request with error message
		}

		[HttpGet("elastic")]
		public async Task<IActionResult> ElasticConnection()
		{
			var pingResponse = await _elasticClient.PingAsync();
			if (pingResponse.IsValid)
			{
				return Ok("Connected to Elasticsearch successfully");
			}
			else
			{
				return StatusCode(500, $"Failed to connect to Elasticsearch: {pingResponse.DebugInformation}");
			}
		}

		[HttpGet("elastic/products")]
		public async Task<IActionResult> SearchProducts(
			[FromQuery(Name = "product-name")] string? name,
			[FromQuery(Name = "min-price")] decimal? minPrice,
			[FromQuery(Name = "max-price")] decimal? maxPrice,
			[FromQuery(Name = "page-number")] int pageNumber = 1,
			[FromQuery(Name = "page-size")] int pageSize = 10)
		{
			var products = await _productService.SearchProductAsync(name, minPrice, maxPrice, pageNumber, pageSize);

			if (products != null && products.Any())
			{
				return Ok(products); // 200 OK with search results
			}

			return NotFound("No products found matching the criteria"); // 404 if no products are found

		}

		[HttpPost("documents")]
		[Authorize(Roles = "ADMIN")]
		public async Task<IActionResult> CreateDocuments()
		{
			var result = await _productService.CreateProductDocumentsAsync();
			var count = await _productService.GetDocumentCount();

			return Ok(new
			{
				Message = result,
				DocumentCount = count,
				Timestamp = DateTime.UtcNow
			});
		}
	}
}
