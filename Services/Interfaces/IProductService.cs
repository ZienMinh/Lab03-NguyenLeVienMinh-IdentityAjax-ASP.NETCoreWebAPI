using BusinessObjects.Entities;
using BusinessObjects.Models.Request;
using BusinessObjects.Models.Response;

namespace Services.Interfaces
{
	public interface IProductService
	{
		Task<BaseResponse<List<Product>>> GetProducts();
		Task<BaseResponse<Product>> FindProductById(int id);
		Task<BaseResponse<Product>> CreateProduct(ProductRequestModel product);
		Task<BaseResponse<Product>> UpdateProduct(int id, ProductRequestModel productRequest);
		Task<BaseResponse<bool>> DeleteProduct(Product product);
		Task<List<Product>> SearchProductAsync(string? name, decimal? minPrice, decimal? maxPrice);
		Task<string> CreateProductDocumentsAsync();
	}
}
