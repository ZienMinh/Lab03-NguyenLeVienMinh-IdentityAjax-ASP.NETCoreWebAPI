using BusinessObjects.Entities;

namespace BusinessObjects.Contracts.Repositories
{
	public interface IProductRepository : IBaseRepository<Product>
	{
		Task<List<Product>> GetProducts();
		Task<Product> GetProductWithCategoryById(int productId);
		Task<Product> FindProductById(int id);
		Task CreateProduct(Product product);
		Task UpdateProduct(Product product);
		Task DeleteProduct(Product product);
		Task<List<Product>> SearchProductAsync(string? name, decimal? minPrice, decimal? maxPrice);
		Task<string> CreateProductDocumentsAsync();
	}
}
