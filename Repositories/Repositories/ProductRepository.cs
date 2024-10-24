using AutoMapper;
using BusinessObjects.Contracts.Repositories;
using BusinessObjects.Entities;
using Microsoft.EntityFrameworkCore;
using Nest;
using Repositories.Context;

namespace Repositories.Repositories
{
	public class ProductRepository : BaseRepository<Product>, IProductRepository
	{
		private readonly ApplicationDbContext _context;
		private readonly IElasticClient _elasticClient;
		private readonly IMapper _mapper;

		public ProductRepository(ApplicationDbContext context, IMapper mapper, IElasticClient elasticClient) : base(context)
		{
			_context = context;
			_elasticClient = elasticClient;
			_mapper = mapper;
		}

		public async Task<List<Product>> GetProducts()
		{
			return await GetQueryable().Include(p => p.Category).ToListAsync();
		}

		public async Task<Product> GetProductWithCategoryById(int productId)
		{
			return await GetQueryable().Include(p => p.Category).FirstOrDefaultAsync(p => p.ProductId == productId);
		}

		public async Task<Product> FindProductById(int id)
		{
			return await GetByIntId(id);
		}

		public async Task CreateProduct(Product product)
		{
			await AddAsync(product);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateProduct(Product product)
		{
			Update(product);
			await _context.SaveChangesAsync();
		}

		public async Task DeleteProduct(Product product)
		{
			Delete(product);
			await _context.SaveChangesAsync();
		}

		public async Task<bool> DeleteData()
		{
			var isExits = await _elasticClient.Indices.ExistsAsync("indexs");
			if (isExits.Exists)
			{
				var response = await _elasticClient.Indices.DeleteAsync("indexs");
				return response.IsValid;
			}
			return true;
		}

		public async Task<string> CreateProductDocumentsAsync()
		{
			try
			{
				var result = await DeleteData();
				if (result)
				{
					var products = _context.Products.ToList();

					var productDTOs = _mapper.Map<List<Product>>(products);

					foreach (var product in products)
					{
						var response = await _elasticClient.IndexDocumentAsync(product);
						if (!response.IsValid)
						{
							return $"Failed to index product {product.ProductId}: {response.ServerError.Error.Reason}";
						}
					}

					return "Products indexed successfully";
				}
				else
				{
					return "Failed to delete old data";
				}
			}
			catch (Exception ex)
			{
				return ex.Message;
			}
		}

		public async Task<List<Product>> SearchProductAsync(string? name, decimal? minPrice, decimal? maxPrice)
		{
			var searchResponse = await _elasticClient.SearchAsync<Product>(s => s
			.Index("indexs")
				.Query(q => q
					.Bool(b =>
					{
						// Search by name if provided
						if (!string.IsNullOrEmpty(name))
						{
							b.Must(m => m.Match(mq => mq.Field(f => f.ProductName).Query(name)));
						}

						if (minPrice.HasValue && maxPrice.HasValue)
						{
							b.Must(m => m.Range(r => r
								.Field(f => f.UnitPrice)
								.GreaterThanOrEquals((double?)minPrice.Value)
								.LessThanOrEquals((double?)maxPrice.Value)));
						}
						return b;
					})
				)
			);

			if (!searchResponse.IsValid)
			{
				Console.WriteLine($"Error: {searchResponse.ServerError?.Error?.Reason}");
				return new List<Product>();
			}

			return searchResponse.Documents.ToList();
		}
	}
}
