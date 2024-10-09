using BusinessObjects.Contracts.Repositories;
using BusinessObjects.Contracts.UnitOfWorks;
using Repositories.Context;
using Repositories.Repositories;
using System.Reflection;

namespace Repositories.UnitOfWorks;

public class BaseUnitOfWork<TContext> : IBaseUnitOfWork
	where TContext : BaseDbContext
{
	private readonly TContext _context;
	private readonly IServiceProvider _serviceProvider;

	protected BaseUnitOfWork(TContext context, IServiceProvider serviceProvider)
	{
		_context = context;
		_serviceProvider = serviceProvider;
	}

	public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		var result = await _context.AsynSaveChangesAsync(cancellationToken);  
		return result > 0;
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing) _context.Dispose();
	}

	public TRepository GetRepository<TRepository>() where TRepository : IBaseRepository
	{
		if (_serviceProvider != null)
		{
			var result = _serviceProvider.GetService(typeof(TRepository));
			return (TRepository)result;
		}
		return default;
	}

	public IBaseRepository<TEntity> GetRepositoryByEntity<TEntity>() where TEntity : class
	{
		var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
		var type = typeof(IBaseRepository<TEntity>);
		foreach (var property in properties)
			if (type.IsAssignableFrom(property.PropertyType))
			{
				var value = (IBaseRepository<TEntity>)property.GetValue(this);
				return value;
			}
		return new BaseRepository<TEntity>(_context as ApplicationDbContext);
	}
}

