using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using PRN231.ExploreNow.BusinessObject.Entities;
using System.Text.Json.Serialization;

namespace BusinessObjects.Entities
{
	public class Product
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int ProductId { get; set; }

		[Required]
		public string UserId { get; set; }

		[JsonIgnore]
		public ApplicationUser User { get; set; }

		[Required]
		[StringLength(40)]
		public string? ProductName { get; set; }

		[Required]
		public int CategoryId { get; set; }

		[Required]
		public int UnitsInStock { get; set; }

		[Required]
		public decimal UnitPrice { get; set; }

		public string CreatedBy { get; set; }

		public DateTime CreatedDate { get; set; }

		public virtual Category? Category { get; set; }

	}
}
