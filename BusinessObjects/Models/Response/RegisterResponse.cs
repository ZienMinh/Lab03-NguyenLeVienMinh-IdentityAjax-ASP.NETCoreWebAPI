using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.Models.Response
{
	public class RegisterResponse
	{
		[Required(ErrorMessage = "FirstName is required")]
		public string FirstName { get; set; }

		[Required(ErrorMessage = "LastName is required")]
		public string LastName { get; set; }

		[Required(ErrorMessage = "UserName is required")]
		public string UserName { get; set; }

		[Required(ErrorMessage = "Email is required")]
		[EmailAddress(ErrorMessage = "Invalid Email Address")]
		public string Email { get; set; }

		[Required(ErrorMessage = "Password is required")]
		public string Password { get; set; }

		[Required(ErrorMessage = "ConfirmPassword is required")]
		[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
		public string ConfirmPassword { get; set; }
	}
}
