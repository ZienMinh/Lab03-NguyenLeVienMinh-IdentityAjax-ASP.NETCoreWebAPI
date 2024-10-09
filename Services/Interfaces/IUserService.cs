namespace Services.Interfaces
{
	public interface IUserService
	{
		Task<bool> VerifyEmailTokenAsync(string email, string token);
	}
}
