using BusinessObjects.Contracts.Repositories;
using Repositories.Repositories.Interfaces;

namespace BusinessObjects.Contracts.UnitOfWorks;

public interface IUnitOfWork : IBaseUnitOfWork
{
	IUserRepository UserRepository { get; }
	ICategoryRepository CategoryRepository { get; }
	IProductRepository ProductRepository { get; }
	IRefreshTokenRepository RefreshTokenRepository { get; }
}
