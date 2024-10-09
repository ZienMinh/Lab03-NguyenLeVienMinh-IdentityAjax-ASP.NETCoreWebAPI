using BusinessObjects.Contracts.Repositories;
using BusinessObjects.Contracts.UnitOfWorks;
using Repositories.Context;
using Repositories.Repositories.Interfaces;
namespace Repositories.UnitOfWorks;

public class UnitOfWork : BaseUnitOfWork<ApplicationDbContext>, IUnitOfWork
{

	public UnitOfWork(ApplicationDbContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
	{
	}

	public IRefreshTokenRepository RefreshTokenRepository => GetRepository<IRefreshTokenRepository>();

	public IUserRepository UserRepository => GetRepository<IUserRepository>();

	public ICategoryRepository CategoryRepository => GetRepository<ICategoryRepository>();

	public IProductRepository ProductRepository => GetRepository<IProductRepository>();
}
