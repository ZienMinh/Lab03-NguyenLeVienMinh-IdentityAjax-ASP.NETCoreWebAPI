using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PRN231.ExploreNow.BusinessObject.Entities;

namespace BusinessObjects.Entities
{
	public class Category
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int CategoryId { get; set; }

		[Required]
		[StringLength(40)]
		public string CategoryName { get; set; }

		[JsonIgnore]
		public virtual ICollection<Product> Products { get; set; }
	}
}
