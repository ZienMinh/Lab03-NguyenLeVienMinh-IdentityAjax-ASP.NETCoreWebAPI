using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PRN231.ExploreNow.BusinessObject.Entities;

namespace Repositories.Context
{
	public abstract class BaseDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
	{
		protected BaseDbContext(DbContextOptions options)
			: base(options)
		{
		}

		public async Task<int> AsynSaveChangesAsync(CancellationToken cancellationToken)
		{
			return await base.SaveChangesAsync();
		}
	}
}
