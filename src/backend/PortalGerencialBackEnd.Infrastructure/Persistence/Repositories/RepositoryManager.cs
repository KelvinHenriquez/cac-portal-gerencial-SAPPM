using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PortalGerencialBackEnd.Core.Interfaces.Repositories;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Repositories
{
    public class RepositoryManager : IRepositoryManager
    {
        private readonly PortalGerencialDbContext _context;

        public RepositoryManager(PortalGerencialDbContext context)
        {
            _context = context;
        }

        public Task<int> SaveAsync() => _context.SaveChangesAsync();

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var result = await action();
                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
    }
}
