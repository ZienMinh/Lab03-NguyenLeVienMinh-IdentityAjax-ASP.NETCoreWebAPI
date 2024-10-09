using BusinessObjects.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Models.Request
{
	public class ProductRequestModel
	{
		[StringLength(40)]
		public string? ProductName { get; set; }

		public int CategoryId { get; set; }

		public int UnitsInStock { get; set; }

		public decimal UnitPrice { get; set; }

		public virtual CategoryRequestModel? Category { get; set; }
	}
}
