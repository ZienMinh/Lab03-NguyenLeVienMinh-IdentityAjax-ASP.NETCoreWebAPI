using BusinessObjects.Entities;
using BusinessObjects.Models.Request;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Models.Response
{
	public class ProductResponseModel
	{
		public int ProductId { get; set; }

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

		public virtual CategoryRequestModel? Category { get; set; }
	}
}
