using System.Security.Claims;

namespace Services.Interfaces
{
	public interface IJwtService
	{
		string GenerateAccessToken(string userId, IList<string> roles);
		string GenerateRefreshToken();
		ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
	}
}
