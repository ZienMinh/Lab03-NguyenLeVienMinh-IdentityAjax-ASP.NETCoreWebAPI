using BusinessObjects.Contracts.UnitOfWorks;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using BusinessObjects.Models.Request;
using BusinessObjects.Models.Response;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using PRN231.ExploreNow.BusinessObject.Entities;
using Repositories.Repositories.Interfaces;
using Services.Interfaces;
using Services.Services;
using System.Security.Claims;
using Xunit;

namespace UnitTests
{
	public class AuthServiceTests
	{
		private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
		private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
		private readonly Mock<ILogger<AuthService>> _loggerMock;
		private readonly Mock<IUnitOfWork> _unitOfWorkMock;
		private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
		private readonly Mock<IJwtService> _jwtServiceMock;
		private readonly AuthService _authService;
		private readonly IConfiguration _configuration;

		public AuthServiceTests()
		{
			// Setup Configuration
			var myConfiguration = new Dictionary<string, string>
			{
				{"JWT:Secret", "aaaaabDDDejExploreNowDSecretKeysnmaasekE"},
				{"JWT:ValidIssuer", "https://localhost:7047"},
				{"JWT:ValidAudience", "https://localhost:3000"}
			};

			_configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(myConfiguration)
				.Build();

			// Setup UserManager mock
			var userStore = new Mock<IUserStore<ApplicationUser>>();
			_userManagerMock = new Mock<UserManager<ApplicationUser>>(
				userStore.Object, null, null, null, null, null, null, null, null);

			// Setup RoleManager mock
			var roleStore = new Mock<IRoleStore<IdentityRole>>();
			_roleManagerMock = new Mock<RoleManager<IdentityRole>>(
				roleStore.Object, null, null, null, null);

			// Setup other mocks
			_loggerMock = new Mock<ILogger<AuthService>>();
			_unitOfWorkMock = new Mock<IUnitOfWork>();
			_refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
			_jwtServiceMock = new Mock<IJwtService>();

			// Setup repository in unit of work
			_unitOfWorkMock.Setup(x => x.GetRepository<IRefreshTokenRepository>())
				.Returns(_refreshTokenRepositoryMock.Object);

			// Create AuthService instance
			_authService = new AuthService(
				_userManagerMock.Object,
				_roleManagerMock.Object,
				_loggerMock.Object,
				_configuration,
				_jwtServiceMock.Object,
				_unitOfWorkMock.Object
			);
		}

		[Fact]
		public async Task SeedRoles_ShouldReturnSuccess_WhenRolesDoNotExist()
		{
			// Arrange
			_roleManagerMock.Setup(x => x.RoleExistsAsync(StaticUserRoles.ADMIN))
				.ReturnsAsync(false);
			_roleManagerMock.Setup(x => x.RoleExistsAsync(StaticUserRoles.CUSTOMER))
				.ReturnsAsync(false);
			_roleManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityRole>()))
				.ReturnsAsync(IdentityResult.Success);

			// Act
			var result = await _authService.SeedRolesAsync();

			// Assert
			result.Should().NotBeNull();
			result.IsSucceed.Should().BeTrue();
			result.Token.Should().Be("Role Seeding Done Successfully");
			_roleManagerMock.Verify(x => x.CreateAsync(It.IsAny<IdentityRole>()), Times.Exactly(2));
		}

		[Fact]
		public async Task Register_ShouldReturnFailure_WhenUserNameExists()
		{
			// Arrange
			var registerRequest = new RegisterResponse
			{
				UserName = "existinguser",
				Email = "test@test.com",
				Password = "Test123!",
				ConfirmPassword = "Test123!"
			};

			_userManagerMock.Setup(x => x.FindByNameAsync(registerRequest.UserName))
				.ReturnsAsync(new ApplicationUser());

			// Act
			var result = await _authService.RegisterAsync(registerRequest);

			// Assert
			result.Should().NotBeNull();
			result.IsSucceed.Should().BeFalse();
			result.Token.Should().Be("UserName Already Exists");
		}

		[Fact]
		public async Task Register_ShouldReturnFailure_WhenEmailExists()
		{
			// Arrange
			var registerRequest = new RegisterResponse
			{
				UserName = "newuser",
				Email = "existing@test.com",
				Password = "Test123!",
				ConfirmPassword = "Test123!"
			};

			_userManagerMock.Setup(x => x.FindByNameAsync(registerRequest.UserName))
				.ReturnsAsync((ApplicationUser)null);
			_userManagerMock.Setup(x => x.FindByEmailAsync(registerRequest.Email))
				.ReturnsAsync(new ApplicationUser());

			// Act
			var result = await _authService.RegisterAsync(registerRequest);

			// Assert
			result.Should().NotBeNull();
			result.IsSucceed.Should().BeFalse();
			result.Token.Should().Be("Email Already Exists");
		}

		[Fact]
		public async Task Login_ShouldReturnSuccess_WhenCredentialsAreValid()
		{
			// Arrange
			var loginRequest = new LoginResponse
			{
				UserName = "testuser",
				Password = "Test123!"
			};

			var user = new ApplicationUser
			{
				Id = "testid",
				UserName = loginRequest.UserName,
				Email = "test@test.com",
				isActived = true,
				FirstName = "Test",
				LastName = "User"
			};

			var userRoles = new List<string> { StaticUserRoles.CUSTOMER };

			_userManagerMock.Setup(x => x.FindByNameAsync(loginRequest.UserName))
				.ReturnsAsync(user);
			_userManagerMock.Setup(x => x.CheckPasswordAsync(user, loginRequest.Password))
				.ReturnsAsync(true);
			_userManagerMock.Setup(x => x.GetRolesAsync(user))
				.ReturnsAsync(userRoles);

			// Setup JWT claims
			var authClaims = new List<Claim>
			{
				new Claim(ClaimTypes.Name, user.UserName),
				new Claim(ClaimTypes.NameIdentifier, user.Id),
				new Claim("JWTID", Guid.NewGuid().ToString()),
				new Claim("FirstName", user.FirstName),
				new Claim("LastName", user.LastName),
				new Claim("email", user.Email)
			};
			authClaims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

			_jwtServiceMock.Setup(x => x.GenerateAccessToken(user.Id, userRoles))
				.Returns("test-access-token");
			_jwtServiceMock.Setup(x => x.GenerateRefreshToken())
				.Returns("test-refresh-token");

			// Setup mock data for refresh token
			var mockData = new List<RefreshToken>().AsQueryable().BuildMockDbSet();
			_refreshTokenRepositoryMock.Setup(x => x.GetQueryable(It.IsAny<CancellationToken>()))
				.Returns(mockData.Object);

			// Setup repository methods
			_refreshTokenRepositoryMock.Setup(x => x.AddAsync(It.IsAny<RefreshToken>()))
				.Returns(Task.FromResult(true));

			_unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true));

			// Act
			var result = await _authService.LoginAsync(loginRequest);

			// Assert
			result.Should().NotBeNull();
			result.IsSucceed.Should().BeTrue();
			result.Token.Should().Be("test-access-token");
			result.RefreshToken.Should().Be("test-refresh-token");
			result.Role.Should().Be(StaticUserRoles.CUSTOMER);
			result.UserId.Should().Be(user.Id);
			result.Email.Should().Be(user.Email);

			// Verify interactions
			_refreshTokenRepositoryMock.Verify(x => x.AddAsync(It.Is<RefreshToken>(rt =>
				rt.Token == "test-refresh-token" &&
				rt.UserId == user.Id)), Times.Once);
			_unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task RefreshToken_ShouldReturnSuccess_WhenTokensAreValid()
		{
			// Arrange
			var refreshRequest = new RefreshTokenRequest
			{
				AccessToken = "valid-access-token",
				RefreshToken = "valid-refresh-token"
			};

			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, "testid")
			};
			var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

			var user = new ApplicationUser
			{
				Id = "testid",
				Email = "test@test.com"
			};

			var refreshTokenEntry = new RefreshToken
			{
				Token = refreshRequest.RefreshToken,
				UserId = "testid",
				ExpiryDate = DateTime.UtcNow.AddDays(1)
			};

			_jwtServiceMock.Setup(x => x.GetPrincipalFromExpiredToken(refreshRequest.AccessToken))
				.Returns(claimsPrincipal);

			var mockData = new List<RefreshToken> { refreshTokenEntry }.AsQueryable().BuildMockDbSet();
			_refreshTokenRepositoryMock.Setup(x => x.GetQueryable(It.IsAny<CancellationToken>()))
				.Returns(mockData.Object);

			_userManagerMock.Setup(x => x.FindByIdAsync("testid"))
				.ReturnsAsync(user);
			_userManagerMock.Setup(x => x.GetRolesAsync(user))
				.ReturnsAsync(new List<string> { StaticUserRoles.CUSTOMER });

			_jwtServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<IList<string>>()))
				.Returns("new-access-token");
			_jwtServiceMock.Setup(x => x.GenerateRefreshToken())
				.Returns("new-refresh-token");

			// Act
			var result = await _authService.RefreshTokenAsync(refreshRequest);

			// Assert
			result.Should().NotBeNull();
			result.IsSucceed.Should().BeTrue();
			result.Token.Should().Be("new-access-token");
			result.RefreshToken.Should().Be("new-refresh-token");
		}
	}
}