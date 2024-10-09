using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Entities
{
	public partial class RefreshToken
	{
		public int Id { get; set; }
		public string Token { get; set; }
		public string UserId { get; set; }
		public DateTime ExpiryDate { get; set; }
		public bool IsRevoked { get; set; }
	}
}
