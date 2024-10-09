using AutoMapper;
using BusinessObjects.Contracts.Repositories;
using Microsoft.EntityFrameworkCore;
using Repositories.Context;

namespace Repositories.Repositories;
public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
{
	private readonly ApplicationDbContext _context;
	protected readonly IMapper _mapper;

	public BaseRepository(ApplicationDbContext context)
	{
		_context = context;
	}

	public BaseRepository(ApplicationDbContext context, IMapper mapper) : this(context)
	{
		_mapper = mapper;
	}

	protected DbSet<TEntity> DbSet => _context.Set<TEntity>();

	public async Task<bool> Check(Guid id)
	{
		return await DbSet.FindAsync(id) != null;
	}

	public IQueryable<TEntity> GetQueryable(CancellationToken cancellationToken = default)
	{
		CheckCancellationToken(cancellationToken);
		return DbSet.AsQueryable();
	}

	public async Task<long> GetTotalCount()
	{
		return await DbSet.LongCountAsync();
	}

	public async Task<IList<TEntity>> GetAll(CancellationToken cancellationToken = default)
	{
		return await GetQueryable(cancellationToken).ToListAsync(cancellationToken);
	}

	public virtual async Task<TEntity> GetById(Guid id)
	{
		return await DbSet.FindAsync(id);
	}

	public virtual async Task<TEntity> GetByIntId(int id)
	{
		return await DbSet.FindAsync(id);
	}

	public virtual async Task<IList<TEntity>> GetByIds(IList<Guid> ids)
	{
		return await DbSet.Where(e => ids.Contains((Guid)e.GetType().GetProperty("Id").GetValue(e))).ToListAsync();
	}

	public void Add(TEntity entity)
	{
		DbSet.Add(entity);
	}

	public async Task AddAsync(TEntity entity)
	{
		await DbSet.AddAsync(entity);
	}

	public void AddRange(IEnumerable<TEntity> entities)
	{
		if (entities.Any())
		{
			DbSet.AddRange(entities);
		}
	}

	public void Update(TEntity entity)
	{
		DbSet.Update(entity);
	}

	public void UpdateRange(IEnumerable<TEntity> entities)
	{
		if (entities.Any())
		{
			DbSet.UpdateRange(entities);
		}
	}

	public void Delete(TEntity entity)
	{
		DbSet.Remove(entity);
	}

	public void DeleteRange(IEnumerable<TEntity> entities)
	{
		DbSet.RemoveRange(entities);
	}

	public void CheckCancellationToken(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			throw new OperationCanceledException("Request was cancelled");
	}
}
