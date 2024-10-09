using BusinessObjects.Entities;

namespace BusinessObjects.Contracts.Repositories
{
	public interface ICategoryRepository : IBaseRepository<Category>
	{
		Task<IList<Category>> GetCategories();
	}
}
