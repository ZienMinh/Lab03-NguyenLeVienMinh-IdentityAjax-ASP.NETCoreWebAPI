using BusinessObjects.Contracts.Repositories;
using BusinessObjects.Entities;
using Repositories.Context;

namespace Repositories.Repositories
{
	public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
	{
		private readonly ApplicationDbContext _context;

		public CategoryRepository(ApplicationDbContext context) : base(context)
		{
			_context = context;
		}

		public async Task<IList<Category>> GetCategories()
		{
			return await GetAll();
		}
	}
}
