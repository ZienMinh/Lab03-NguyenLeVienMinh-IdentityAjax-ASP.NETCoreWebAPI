using BusinessObjects.Entities;
using Repositories.Context;
using Repositories.Repositories.Interfaces;

namespace Repositories.Repositories
{
	public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
	{
		private readonly ApplicationDbContext _context;

		public RefreshTokenRepository(ApplicationDbContext context) : base(context)
		{
			_context = context;
		}
	}
}
