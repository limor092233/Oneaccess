using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Storage;
using OneAccess.Application.Common.Interfaces;

namespace OneAccess.Infrastructure.Persistence.Repositories;

/// <summary>
/// Unit of Work implementation controlling transactional boundaries and commits.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly OneAccessDbContext _dbContext;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private IDbContextTransaction? _transaction;

    public UnitOfWork(OneAccessDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IRepository<T> Repository<T>() where T : class
    {
        return (IRepository<T>)_repositories.GetOrAdd(typeof(T), _ => new Repository<T>(_dbContext));
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _dbContext.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null) return;
        _transaction = await _dbContext.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            if (_transaction != null)
            {
                await _transaction.CommitAsync(ct);
            }
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(ct);
            }
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
