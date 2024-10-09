using BusinessObjects.Contracts.Repositories;

namespace BusinessObjects.Contracts.UnitOfWorks;
public interface IBaseUnitOfWork : IDisposable
{
	IBaseRepository<TEntity> GetRepositoryByEntity<TEntity>() where TEntity : class;
	TRepository GetRepository<TRepository>() where TRepository : IBaseRepository;
	Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
