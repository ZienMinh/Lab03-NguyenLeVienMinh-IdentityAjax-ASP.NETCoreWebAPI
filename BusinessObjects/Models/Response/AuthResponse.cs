namespace BusinessObjects.Models.Response
{
	public class AuthResponse
	{
		public bool IsSucceed { get; set; }
		public string? Token { get; set; }
		public string? Role { get; set; }
		public string RefreshToken { get; set; }
		public string? UserId { get; set; }
		public string? Email { get; set; }
	}
}
