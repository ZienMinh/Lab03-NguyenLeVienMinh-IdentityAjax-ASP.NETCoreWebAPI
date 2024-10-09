using BusinessObjects.Contracts.UnitOfWorks;
using BusinessObjects.Entities;
using BusinessObjects.Models.Request;
using BusinessObjects.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using PRN231.ExploreNow.BusinessObject.Entities;
using Services.Interfaces;

namespace Services.Services
{
	public class ProductService : IProductService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IHttpContextAccessor _contextAccessor;
		public ProductService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IHttpContextAccessor contextAccessor)
		{
			_unitOfWork = unitOfWork;
			_userManager = userManager;
			_contextAccessor = contextAccessor;
		}

		public async Task<BaseResponse<List<Product>>> GetProducts()
		{
			try
			{
				var products = await _unitOfWork.ProductRepository.GetProducts();
				return new BaseResponse<List<Product>>
				{
					IsSucceed = true,
					Result = products,
					Message = "Products retrieved successfully"
				};
			}
			catch (Exception e)
			{
				return new BaseResponse<List<Product>>
				{
					IsSucceed = false,
					Message = $"Error retrieving products: {e.Message}"
				};
			}
		}

		public async Task<BaseResponse<Product>> FindProductById(int id)
		{
			try
			{
				var product = await _unitOfWork.ProductRepository.GetProductWithCategoryById(id);
				//var productResponseMap = MapToProductResponse(product);

				return new BaseResponse<Product>
				{
					IsSucceed = true,
					Result = product,
					Message = "Product retrieved successfully"
				};
			}
			catch (Exception e)
			{
				return new BaseResponse<Product>
				{
					IsSucceed = false,
					Message = $"Error retrieving product: {e.Message}"
				};
			}
		}

		public async Task<BaseResponse<Product>> CreateProduct(ProductRequestModel product)
		{
			try
			{
				var user = await _userManager.GetUserAsync(_contextAccessor.HttpContext.User);
				if (user == null)
				{
					return new BaseResponse<Product>
					{
						IsSucceed = false,
						Message = "User not authenticated."
					};
				}

				var productRequestMap = MapToProduct(product);

				productRequestMap.UserId = user.Id;
				productRequestMap.CreatedBy = user.UserName;
				productRequestMap.CreatedDate = DateTime.UtcNow;

				await _unitOfWork.ProductRepository.CreateProduct(productRequestMap);
				await _unitOfWork.SaveChangesAsync();

				// Get ProductId  when client create Product 
				var productWithCategory = await _unitOfWork.ProductRepository.GetProductWithCategoryById(productRequestMap.ProductId);

				if (productWithCategory == null)
				{
					return new BaseResponse<Product>
					{
						IsSucceed = false,
						Message = "Product not found after creation."
					};
				}

				// Map Product to Product Response Model
				var productResponseMap = MapToProductResponse(productWithCategory);

				return new BaseResponse<Product>
				{
					IsSucceed = true,
					Result = productRequestMap,
					Message = "Product saved successfully"
				};
			}
			catch (Exception e)
			{
				return new BaseResponse<Product>
				{
					IsSucceed = false,
					Message = $"Error saving product: {e.Message} - {e.InnerException?.Message}"
				};
			}
		}

		// Mapping from Request Model to Product Entity
		private Product MapToProduct(ProductRequestModel productRequest)
		{
			return new Product
			{
				ProductName = productRequest.ProductName,
				UnitPrice = productRequest.UnitPrice,
				UnitsInStock = productRequest.UnitsInStock,
				CategoryId = productRequest.CategoryId,
			};
		}

		// Mapping from Product Entity to Response Model
		private ProductResponseModel MapToProductResponse(Product product)
		{
			return new ProductResponseModel
			{
				ProductId = product.ProductId,
				ProductName = product.ProductName,
				UnitPrice = product.UnitPrice,
				UnitsInStock = product.UnitsInStock,
				CategoryId = product.CategoryId,
				Category = product.Category != null ? new CategoryRequestModel
				{
					CategoryName = product.Category.CategoryName
				} : null
			};
		}

		public async Task<BaseResponse<Product>> UpdateProduct(int id, ProductRequestModel productRequest)
		{
			try
			{
				var existingProduct = await _unitOfWork.ProductRepository.GetProductWithCategoryById(id);

				if (existingProduct == null)
				{
					return new BaseResponse<Product>
					{
						IsSucceed = false,
						Message = "Product not found"
					};
				}

				existingProduct.ProductName = productRequest.ProductName;
				existingProduct.UnitPrice = productRequest.UnitPrice;
				existingProduct.UnitsInStock = productRequest.UnitsInStock;
				existingProduct.CategoryId = productRequest.CategoryId;

				await _unitOfWork.ProductRepository.UpdateProduct(existingProduct);
				await _unitOfWork.SaveChangesAsync();

				return new BaseResponse<Product>
				{
					IsSucceed = true,
					Result = existingProduct,
					Message = "Product updated successfully"
				};
			}
			catch (Exception e)
			{
				return new BaseResponse<Product>
				{
					IsSucceed = false,
					Message = $"Error saving product: {e.Message} - {e.InnerException?.Message}"
				};
			}
		}

		public async Task<BaseResponse<bool>> DeleteProduct(Product product)
		{
			try
			{
				await _unitOfWork.ProductRepository.DeleteProduct(product);
				await _unitOfWork.SaveChangesAsync();
				return new BaseResponse<bool>
				{
					IsSucceed = true,
					Result = true,
					Message = "Product deleted successfully"
				};
			}
			catch (Exception e)
			{
				return new BaseResponse<bool>
				{
					IsSucceed = false,
					Message = $"Error deleting product: {e.Message}"
				};
			}
		}

		public async Task<List<Product>> SearchProductAsync(string? name, decimal? minPrice, decimal? maxPrice)
		{
			return await _unitOfWork.ProductRepository.SearchProductAsync(name, minPrice, maxPrice);
		}

		public async Task<string> CreateProductDocumentsAsync()
		{
			return await _unitOfWork.ProductRepository.CreateProductDocumentsAsync();
		}
	}
}
