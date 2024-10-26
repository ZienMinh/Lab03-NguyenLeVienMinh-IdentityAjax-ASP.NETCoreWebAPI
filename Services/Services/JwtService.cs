using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Services.Services
{
	public class JwtService : IJwtService
	{
		private readonly IConfiguration _config;

		public JwtService(IConfiguration config)
		{
			_config = config;
		}
		public string GenerateAccessToken(string userId, string role)
		{
			return GenerateAccessToken(userId, new List<string> { role });
		}

		public string GenerateAccessToken(string userId, IList<string> roles)
		{
			var claims = new List<Claim>
			{
				new Claim(JwtRegisteredClaimNames.Sub, userId),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			};

			foreach (var role in roles)
			{
				claims.Add(new Claim(ClaimTypes.Role, role));
			}

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Secret"]));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: _config["JWT:ValidIssuer"],
				audience: _config["JWT:ValidAudience"],
				claims: claims,
				expires: DateTime.UtcNow.AddMinutes(30),
				signingCredentials: creds);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}

		// Generates a secure random refresh token
		public string GenerateRefreshToken()
		{
			// Generates a 64-byte refresh token as a base64-encoded string
			return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
		}

		// Extracts the claims principal from an expired token (validates signature, issuer, audience, but not lifetime)
		public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
		{
			var tokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_config["JWT:Secret"])),
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidateLifetime = false, // We are not checking the expiration here, so set it to false
				ValidIssuer = _config["JWT:ValidIssuer"],
				ValidAudience = _config["JWT:ValidAudience"]
			};

			var tokenHandler = new JwtSecurityTokenHandler();
			SecurityToken securityToken;
			var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
			var jwtToken = securityToken as JwtSecurityToken;

			// Check if the token is valid and uses the correct signing algorithm
			if (jwtToken == null || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
				throw new SecurityTokenException("Invalid token");

			return principal; // Return the ClaimsPrincipal extracted from the token
		}
	}
}
