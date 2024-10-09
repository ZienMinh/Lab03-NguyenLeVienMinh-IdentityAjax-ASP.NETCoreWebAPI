using System.Security.Cryptography;

namespace BusinessObjects.Models.Request
{
	public class TokenGenerator
	{
		public static Func<string> CreateRandomTokenDelegate = DefaultCreateRandomToken;

		public static string CreateRandomToken()
		{
			//return Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
			return CreateRandomTokenDelegate();
		}
		private static string DefaultCreateRandomToken()
		{
			return Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
		}
	}
}
