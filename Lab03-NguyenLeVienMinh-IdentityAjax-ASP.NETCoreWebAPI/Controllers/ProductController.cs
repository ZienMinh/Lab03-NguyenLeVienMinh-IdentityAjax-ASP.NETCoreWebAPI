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

		// GET: api/products
		[HttpGet]
		[Authorize]
		public async Task<IActionResult> GetAllProducts()
		{
			var response = await _productService.GetProducts();
			if (response.IsSucceed)
			{
				return Ok(response.Result);
			}
			return BadRequest(response.Message); // 400 Bad Request with error message
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
		public async Task<IActionResult> SearchProducts([FromQuery] string? name, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice)
		{
			var products = await _productService.SearchProductAsync(name, minPrice, maxPrice);

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
			var documents = await _productService.CreateProductDocumentsAsync();

			if (documents.Contains("Products indexed successfully"))
			{
				return Ok(documents); // 200 OK with success message
			}

			return BadRequest(documents);
		}
	}
}
