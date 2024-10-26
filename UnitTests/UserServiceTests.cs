using BusinessObjects.Contracts.UnitOfWorks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using PRN231.ExploreNow.BusinessObject.Entities;
using Repositories.Repositories.Interfaces;
using Services.Services;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using Xunit;
using FluentAssertions;

namespace UnitTests
{
	public class UserServiceTests
	{
		private readonly Mock<IUnitOfWork> _unitOfWorkMock;
		private readonly Mock<IUserRepository> _userRepositoryMock;
		private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
		private readonly Mock<IConfiguration> _mockConfiguration;
		private readonly Mock<IHttpContextAccessor> _mockContextAccessor;
		private readonly UserService _userService;

		public UserServiceTests()
		{
			_unitOfWorkMock = new Mock<IUnitOfWork>();
			_userRepositoryMock = new Mock<IUserRepository>();
			_mockConfiguration = new Mock<IConfiguration>();
			var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
			_mockUserManager = new Mock<UserManager<ApplicationUser>>(mockUserStore.Object, null, null, null, null, null, null, null, null);
			_mockContextAccessor = new Mock<IHttpContextAccessor>();

			_unitOfWorkMock.Setup(uow => uow.GetRepository<IUserRepository>())
				.Returns(_userRepositoryMock.Object);

			_userService = new UserService(_unitOfWorkMock.Object);
		}

		[Fact]
		public async Task VerifyEmailTokenAsync_ShouldReturnFalse_WhenUserNotFound()
		{
			// Arange
			_userRepositoryMock.Setup(repo => repo.GetUserByEmailAsync(It.IsAny<string>()))
				.ReturnsAsync((ApplicationUser)null);

			// Act
			var result = await _userService.VerifyEmailTokenAsync("test@example.com", "token");

			// Assert
			result.Should().BeFalse();
			_unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Never());
		}

		[Fact]
		public async Task VerifyEmailTokenAsync_ShouldReturnFalse_WhenTokenDoesNotMatch()
		{
			// Arrange
			var user = new ApplicationUser
			{
				VerifyToken = "invalid-token",
				isActived = false,
			};
			_userRepositoryMock.Setup(repo => repo.GetUserByEmailAsync(It.IsAny<string>()))
				.ReturnsAsync(user);

			// Act
			var result = await _userService.VerifyEmailTokenAsync("test@example.com", "valid-token");

			// Assert
			result.Should().BeFalse();
			_unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task VerifyEmailTokenAsync_ShouldReturnFalse_VerifyEmailTokenAsync_ShouldReturnFalse_WhenUserIsAlreadyActivated()
		{
			// Arrange
			var user = new ApplicationUser
			{
				VerifyToken = "valid-token",
				isActived = true,
			};
			_userRepositoryMock.Setup(repo => repo.GetUserByEmailAsync(It.IsAny<string>()))
							   .ReturnsAsync(user);

			// Act
			var result = await _userService.VerifyEmailTokenAsync("test@example.com", "valid-token");

			// Assert
			result.Should().BeFalse();
			_unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task VerifyEmailTokenAsync_ShouldActivateUser_WhenTokenIsValid()
		{
			// Arrange
			var user = new ApplicationUser
			{
				VerifyToken = "valid-token",
				isActived = false,
				VerifyTokenExpires = DateTime.UtcNow.AddMinutes(30)
			};
			_userRepositoryMock.Setup(repo => repo.GetUserByEmailAsync(It.IsAny<string>()))
							   .ReturnsAsync(user);

			// Act
			var result = await _userService.VerifyEmailTokenAsync("test@example.com", "valid-token");

			// Assert
			result.Should().BeTrue();
			user.isActived.Should().BeTrue();
			user.VerifyToken.Should().BeNull();
			user.VerifyTokenExpires.Should().Be(DateTime.MinValue);
			_userRepositoryMock.Verify(repo => repo.Update(user), Times.Once);
			_unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
		}
	}
}