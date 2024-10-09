namespace BusinessObjects.Contracts.Repositories;
public interface IBaseRepository
{

}

public interface IBaseRepository<TEntity> : IBaseRepository 
	where TEntity : class
{
	Task<bool> Check(Guid id);
	IQueryable<TEntity> GetQueryable(CancellationToken cancellationToken = default);
	Task<long> GetTotalCount();
	Task<IList<TEntity>> GetAll(CancellationToken cancellationToken = default);
	Task<TEntity> GetById(Guid id);
	Task<IList<TEntity>> GetByIds(IList<Guid> ids);
	void Add(TEntity entity);
	Task AddAsync(TEntity entity);
	void AddRange(IEnumerable<TEntity> entities);
	void Update(TEntity entity);
	void UpdateRange(IEnumerable<TEntity> entities);
	void Delete(TEntity entity);
	void DeleteRange(IEnumerable<TEntity> entities);
	void CheckCancellationToken(CancellationToken cancellationToken = default);
}
