using BusinessObjects.Entities;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace PRN231.ExploreNow.BusinessObject.Entities;

public class ApplicationUser : IdentityUser<string>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? VerifyToken { get; set; }
    public DateTime? VerifyTokenExpires { get; set; }
    public bool isActived { get; set; } = false;

	[JsonIgnore]
	public ICollection<Product> Products { get; set; } = new List<Product>();
}