using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.Models.Response
{
	public class UpdatePermissionResponse
	{
		[Required(ErrorMessage = "UserName is required")]
		public string UserName { get; set; }
	}
}
