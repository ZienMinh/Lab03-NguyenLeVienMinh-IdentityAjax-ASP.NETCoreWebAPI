using BusinessObjects.Contracts.Repositories;
using BusinessObjects.Contracts.UnitOfWorks;
using BusinessObjects.Entities;
using BusinessObjects.Models.Request;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using PRN231.ExploreNow.BusinessObject.Entities;
using Services.Services;
using System.Security.Claims;
using Xunit;

namespace UnitTests
{
	public class ProductServiceTests
	{
		private readonly Mock<IUnitOfWork> _unitOfWorkMock;
		private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
		private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
		private readonly Mock<IProductRepository> _productRepositoryMock;
		private readonly ProductService _productService;
		private readonly Mock<HttpContext> _httpContextMock;

		public ProductServiceTests()
		{
			// Setup mocks
			_unitOfWorkMock = new Mock<IUnitOfWork>();
			_productRepositoryMock = new Mock<IProductRepository>();
			var userStore = new Mock<IUserStore<ApplicationUser>>();
			_userManagerMock = new Mock<UserManager<ApplicationUser>>(
				userStore.Object, null, null, null, null, null, null, null, null);
			_httpContextAccessorMock = new Mock<IHttpContextAccessor>();
			_httpContextMock = new Mock<HttpContext>();

			// Setup IUnitOfWork mock
			_unitOfWorkMock.Setup(u => u.GetRepository<IProductRepository>())
				.Returns(_productRepositoryMock.Object);

			// Create ProductService instance
			_productService = new ProductService(
				_unitOfWorkMock.Object,
				_userManagerMock.Object,
				_httpContextAccessorMock.Object);
		}

		[Fact]
		public async Task GetProducts_ShouldReturnSuccessResponse_WhenProductsExist()
		{
			// Arrange
			var expectedProducts = new List<Product>
			{
				new Product { ProductId = 1, ProductName = "Test Product 1" },
				new Product { ProductId = 2, ProductName = "Test Product 2" }
			};

			_productRepositoryMock.Setup(r => r.GetProducts())
				.ReturnsAsync(expectedProducts);

			// Act
			var result = await _productService.GetProducts();

			// Assert
			result.Should().NotBeNull();
			result.IsSucceed.Should().BeTrue();
			result.Result.Should().BeEquivalentTo(expectedProducts);
			result.Message.Should().Be("Products retrieved successfully");
		}

		[Fact]
		public async Task GetProducts_ShouldReturnFailureResponse_WhenExceptionOccurs()
		{
			// Arrange
			_productRepositoryMock.Setup(r => r.GetProducts())
				.ThrowsAsync(new Exception("Database error"));

			// Act
			var result = await _productService.GetProducts();

			// Assert
			result.Should().NotBeNull();
			result.IsSucceed.Should().BeFalse();
			result.Result.Should().BeNull();
			result.Message.Should().Contain("Error retrieving products");
		}

		[Fact]
		public async Task FindProductById_ShouldReturnProduct_WhenProductExists()
		{
			// Arrange
			var expectedProduct = new Product
			{
				ProductId = 1,
				ProductName = "Test Product",
				Category = new Category { CategoryName = "Test Category" }
			};

			_productRepositoryMock.Setup(r => r.GetProductWithCategoryById(1))
				.ReturnsAsync(expectedProduct);

			// Act
			var result = await _productService.FindProductById(1);

			// Assert
			result.Should().NotBeNull();
			result.IsSucceed.Should().BeTrue();
			result.Result.Should().BeEquivalentTo(expectedProduct);
			result.Message.Should().Be("Product retrieved successfully");
		}

		[Fact]
		public async Task CreateProduct_ShouldReturnSuccess_WhenValidProductProvided()
		{
			// Arrange
			var user = new ApplicationUser { Id = "testUserId", UserName = "testUser" };
			var productRequest = new ProductRequestModel
			{
				ProductName = "New Product",
				UnitPrice = 10.99m,
				UnitsInStock = 100,
				CategoryId = 1
			};

			_httpContextMock.Setup(c => c.User).Returns(new ClaimsPrincipal());
			_httpContextAccessorMock.Setup(a => a.HttpContext).Returns(_httpContextMock.Object);
			_userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync(user);

			var createdProduct = new Product
			{
				ProductId = 1,
				ProductName = productRequest.ProductName,
				UnitPrice = productRequest.UnitPrice,
				UnitsInStock = productRequest.UnitsInStock,
				CategoryId = productRequest.CategoryId,
				UserId = user.Id,
				CreatedBy = user.UserName
			};

			_productRepositoryMock.Setup(r => r.GetProductWithCategoryById(It.IsAny<int>()))
				.ReturnsAsync(createdProduct);

			// Act
			var result = await _productService.CreateProduct(productRequest);

			// Assert
			result.Should().NotBeNull();
			result.IsSucceed.Should().BeTrue();
			result.Message.Should().Be("Product saved successfully");
			result.Result.Should().NotBeNull();
			result.Result.ProductName.Should().Be(productRequest.ProductName);
		}

		[Fact]
		public async Task UpdateProduct_ShouldReturnSuccess_WhenProductExists()
		{
			// Arrange
			var productId = 1;
			var existingProduct = new Product
			{
				ProductId = productId,
				ProductName = "Old Name",
				UnitPrice = 9.99m,
				UnitsInStock = 50,
				CategoryId = 1
			};

			var updateRequest = new ProductRequestModel
			{
				ProductName = "Updated Name",
				UnitPrice = 19.99m,
				UnitsInStock = 100,
				CategoryId = 2
			};

			_productRepositoryMock.Setup(r => r.GetProductWithCategoryById(productId))
				.ReturnsAsync(existingProduct);

			// Act
			var result = await _productService.UpdateProduct(productId, updateRequest);

			// Assert
			result.Should().NotBeNull();
			result.IsSucceed.Should().BeTrue();
			result.Message.Should().Be("Product updated successfully");
			result.Result.ProductName.Should().Be(updateRequest.ProductName);
			result.Result.UnitPrice.Should().Be(updateRequest.UnitPrice);
			result.Result.UnitsInStock.Should().Be(updateRequest.UnitsInStock);
			result.Result.CategoryId.Should().Be(updateRequest.CategoryId);
		}

		[Fact]
		public async Task DeleteProduct_ShouldReturnSuccess_WhenProductExists()
		{
			// Arrange
			var product = new Product { ProductId = 1, ProductName = "Test Product" };

			// Act
			var result = await _productService.DeleteProduct(product);

			// Assert
			result.Should().NotBeNull();
			result.IsSucceed.Should().BeTrue();
			result.Message.Should().Be("Product deleted successfully");
			result.Result.Should().BeTrue();
			_productRepositoryMock.Verify(r => r.DeleteProduct(product), Times.Once);
			_unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task SearchProduct_ShouldReturnMatchingProducts()
		{
			// Arrange
			var expectedProducts = new List<Product>
			{
				new Product { ProductId = 1, ProductName = "Test Product", UnitPrice = 10.99m }
			};

			_productRepositoryMock.Setup(r => r.SearchProductAsync(
				It.IsAny<string>(),
				It.IsAny<decimal?>(),
				It.IsAny<decimal?>(),
				It.IsAny<int>(),
				It.IsAny<int>()))
				.ReturnsAsync(expectedProducts);

			// Act
			var result = await _productService.SearchProductAsync("Test", 10m, 20m, 1, 10);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEquivalentTo(expectedProducts);
			_productRepositoryMock.Verify(r => r.SearchProductAsync("Test", 10m, 20m, 1, 10), Times.Once);
		}
	}
}
