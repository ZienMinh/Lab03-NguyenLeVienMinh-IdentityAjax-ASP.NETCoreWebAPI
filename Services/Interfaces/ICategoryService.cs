using BusinessObjects.Entities;
using BusinessObjects.Models.Response;

namespace Services.Interfaces
{
	public interface ICategoryService
	{
		Task<BaseResponse<IList<Category>>> GetCategories();
	}
}
