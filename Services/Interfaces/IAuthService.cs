using BusinessObjects.Models.Request;
using BusinessObjects.Models.Response;

namespace Services.Interfaces
{
	public interface IAuthService
	{
		Task<AuthResponse> SeedRolesAsync();
		Task<AuthResponse> LoginAsync(LoginResponse loginResponse);
		Task<AuthResponse> RegisterAsync(RegisterResponse registerResponse);
		Task<AuthResponse> MakeAdminAsync(UpdatePermissionResponse updatePermissionResponse);
		Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest refresh);
	}
}
