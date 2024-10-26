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

		//public async Task<bool> DeleteData()
		//{
		//	var isExits = await _elasticClient.Indices.ExistsAsync("indexs");
		//	if (isExits.Exists)
		//	{
		//		var response = await _elasticClient.Indices.DeleteAsync("indexs");
		//		return response.IsValid;
		//	}
		//	return true;
		//}

		//public async Task<string> CreateProductDocumentsAsync()
		//{
		//	try
		//	{
		//		var result = await DeleteData();
		//		if (result)
		//		{
		//			var products = await _context.Products.ToListAsync();

		//			var productDTOs = _mapper.Map<List<Product>>(products);

		//			var bulkResponse = await _elasticClient.BulkAsync(b => b
		//				.Index("indexs")
		//				.CreateMany(productDTOs)
		//				.Refresh(Elasticsearch.Net.Refresh.WaitFor)
		//			 );

		//			if (bulkResponse.IsValid)
		//			{
		//				return $"Successfully indexed {productDTOs.Count} products";
		//			}
		//			foreach (var product in products)
		//			{
		//				var response = await _elasticClient.IndexDocumentAsync(product);
		//				if (!response.IsValid)
		//				{
		//					return $"Failed to index product {product.ProductId}: {response.ServerError.Error.Reason}";
		//				}
		//			}

		//			return "Products indexed successfully";
		//		}
		//		else
		//		{
		//			return "Failed to delete old data";
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		return ex.Message;
		//	}
		//}

		public async Task<bool> DeleteData()
		{
			try
			{
				var isExits = await _elasticClient.Indices.ExistsAsync("indexs");
				if (isExits.Exists)
				{
					var deleteResponse = await _elasticClient.Indices.DeleteAsync("indexs");
					if (!deleteResponse.IsValid)
					{
						return false;
					}
				}

				// Tạo index mới với mapping rõ ràng
				var createResponse = await _elasticClient.Indices.CreateAsync("indexs", c => c
					.Settings(s => s
						.NumberOfShards(1)
						.NumberOfReplicas(1)
						.Setting("max_result_window", 10000) // Cho phép search nhiều kết quả hơn
					)
					.Map<Product>(m => m
						.Properties(ps => ps
							.Keyword(k => k.Name(n => n.ProductId))
							.Text(t => t.Name(n => n.ProductName)
								.Fields(f => f
									.Keyword(k => k.Name("keyword"))))
							.Number(n => n.Name(n => n.UnitPrice))
							.Number(n => n.Name(n => n.UnitsInStock))
							.Date(d => d.Name(n => n.CreatedDate))
							.Keyword(k => k.Name(n => n.CreatedBy))
							.Keyword(k => k.Name(n => n.UserId))
						)
					)
				);

				return createResponse.IsValid;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		public async Task<string> CreateProductDocumentsAsync()
		{
			try
			{
				// Xóa và tạo lại index với mapping
				var result = await DeleteData();
				if (!result)
				{
					return "Failed to prepare index";
				}

				// Lấy tất cả products
				var products = await _context.Products.ToListAsync();
				if (!products.Any())
				{
					return "No products found to index";
				}

				// Thực hiện bulk index với batch size
				var bulkAll = _elasticClient.BulkAll(products, b => b
					.Index("indexs")
					.BackOffTime(TimeSpan.FromSeconds(15))
					.BackOffRetries(2)
					.Size(1000) // Số lượng documents trong mỗi bulk request
					.RefreshOnCompleted()
					.MaxDegreeOfParallelism(Environment.ProcessorCount)
					.ContinueAfterDroppedDocuments()
				);

				var waitHandle = new CountdownEvent(1);
				var documents = 0;

				bulkAll.Subscribe(new BulkAllObserver(
					onNext: response =>
					{
						documents += response.Items.Count;
					},
					onError: error =>
					{
						waitHandle.Signal();
					},
					onCompleted: () => waitHandle.Signal()
				));

				waitHandle.Wait();

				// Đợi một chút để đảm bảo refresh đã hoàn tất
				await Task.Delay(1000);

				// Kiểm tra số lượng documents đã được index
				var count = await GetDocumentCount();
				if (count != products.Count)
				{
					return $"Warning: Only {count} of {products.Count} products were indexed";
				}

				return "Products indexed successfully";
			}
			catch (Exception ex)
			{
				return $"Error during indexing: {ex.Message}";
			}
		}

		public async Task<long> GetDocumentCount()
		{
			var countResponse = await _elasticClient.CountAsync<Product>(c => c
				.Index("indexs")
			);

			return countResponse.Count;
		}

		public async Task<List<Product>> SearchProductAsync(string? name, decimal? minPrice, decimal? maxPrice, int pageNumber = 1, int pageSize = 10)
		{
			pageNumber = Math.Max(1, pageNumber);
			pageSize = Math.Max(1, pageSize);

			var searchResponse = await _elasticClient.SearchAsync<Product>(s => s
			.Index("indexs")
			.From((pageNumber - 1) * pageSize) // Skip
			.Size(pageSize)					   // Take
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
				.TrackTotalHits() // Đếm tổng số records
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
