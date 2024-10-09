using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Models.Request
{
	public class CategoryRequestModel
	{
		[Required]
		[StringLength(40)]
		public string CategoryName { get; set; }
	}
}
