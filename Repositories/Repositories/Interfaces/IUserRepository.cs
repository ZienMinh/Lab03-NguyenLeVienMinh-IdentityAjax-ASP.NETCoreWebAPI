using BusinessObjects.Contracts.Repositories;
using PRN231.ExploreNow.BusinessObject.Entities;
namespace Repositories.Repositories.Interfaces
{
	public interface IUserRepository : IBaseRepository
	{
		Task<ApplicationUser> GetUserByEmailAsync(string email);
		Task Update(ApplicationUser applicationUser);
		Task<ApplicationUser> GetById(Guid id);
	}
}
