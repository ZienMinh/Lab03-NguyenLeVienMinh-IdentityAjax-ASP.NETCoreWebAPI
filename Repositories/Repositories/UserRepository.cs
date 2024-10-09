using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PRN231.ExploreNow.BusinessObject.Entities;
using Repositories.Context;
using Repositories.Repositories.Interfaces;

namespace Repositories.Repositories
{
	public class UserRepository : IUserRepository
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;

		public UserRepository(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
		{
			_userManager = userManager;
			_context = context;
			_httpContextAccessor = httpContextAccessor;
		}

		public async Task<ApplicationUser> GetUserByEmailAsync(string email)
		{
			return await _context.Users
				.AsNoTracking()
				.SingleOrDefaultAsync(u => u.Email == email);
		}

		public async Task Update(ApplicationUser applicationUser)
		{
			_context.Users.Update(applicationUser);
			await _context.SaveChangesAsync();
		}

		public async Task<ApplicationUser> GetById(Guid id)
		{
			return await _userManager.FindByIdAsync(id.ToString());
		}
	}
}
