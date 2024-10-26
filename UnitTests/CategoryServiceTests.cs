using BusinessObjects.Contracts.UnitOfWorks;
using BusinessObjects.Entities;
using Moq;
using FluentAssertions;
using Xunit;
using BusinessObjects.Contracts.Repositories;
using Services.Services;

namespace UnitTests
{
	public class CategoryServiceTests
	{
		private readonly Mock<IUnitOfWork> _unitOfWorkMock;
		private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
		private readonly CategoryService _categoryService;

		public CategoryServiceTests()
		{
			// Setup mocks
			_unitOfWorkMock = new Mock<IUnitOfWork>();
			_categoryRepositoryMock = new Mock<ICategoryRepository>();

			// Setup IUnitOfWork mock
			_unitOfWorkMock.Setup(u => u.GetRepository<ICategoryRepository>())
				.Returns(_categoryRepositoryMock.Object);

			// Create CategoryService instance
			_categoryService = new CategoryService(_unitOfWorkMock.Object);
		}

		[Fact]
		public async Task GetCategories_ShouldReturnSuccessResponse_WhenCategoriesExist()
		{
			// Arrange
			var expectedCategories = new List<Category>
			{
				new Category { CategoryId = 1, CategoryName = "Electronics" },
				new Category { CategoryId = 2, CategoryName = "Books" },
				new Category { CategoryId = 3, CategoryName = "Clothing" }
			};

			_categoryRepositoryMock.Setup(r => r.GetCategories())
				.ReturnsAsync(expectedCategories);

			// Act
			var result = await _categoryService.GetCategories();

			// Assert
			result.Should().NotBeNull();
			result.IsSucceed.Should().BeTrue();
			result.Message.Should().Be("Categories retrieved successfully");
			result.Result.Should().NotBeNull();
			result.Result.Should().BeEquivalentTo(expectedCategories);
			result.Result.Count.Should().Be(3);
		}

		[Fact]
		public async Task GetCategories_ShouldReturnSuccessResponse_WhenNoCategoriesExist()
		{
			// Arrange
			var emptyCategories = new List<Category>();

			_categoryRepositoryMock.Setup(r => r.GetCategories())
				.ReturnsAsync(emptyCategories);

			// Act
			var result = await _categoryService.GetCategories();

			// Assert
			result.Should().NotBeNull();
			result.IsSucceed.Should().BeTrue();
			result.Message.Should().Be("Categories retrieved successfully");
			result.Result.Should().NotBeNull();
			result.Result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetCategories_ShouldReturnFailureResponse_WhenExceptionOccurs()
		{
			// Arrange
			var expectedErrorMessage = "Database connection failed";
			_categoryRepositoryMock.Setup(r => r.GetCategories())
				.ThrowsAsync(new Exception(expectedErrorMessage));

			// Act
			var result = await _categoryService.GetCategories();

			// Assert
			result.Should().NotBeNull();
			result.IsSucceed.Should().BeFalse();
			result.Message.Should().Contain("Error retrieving categories");
			result.Message.Should().Contain(expectedErrorMessage);
			result.Result.Should().BeNull();
		}

		[Fact]
		public async Task GetCategories_ShouldCallRepository_OnlyOnce()
		{
			// Arrange
			_categoryRepositoryMock.Setup(r => r.GetCategories())
				.ReturnsAsync(new List<Category>());

			// Act
			await _categoryService.GetCategories();

			// Assert
			_categoryRepositoryMock.Verify(r => r.GetCategories(), Times.Once);
		}
	}
}