using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PortalGerencialBackEnd.Core.Interfaces.Repositories;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Repositories
{
    public class RepositoryBase<T> : IRepositoryBase<T> where T : class
    {
        protected readonly PortalGerencialDbContext Context;

        public RepositoryBase(PortalGerencialDbContext context)
        {
            Context = context;
        }

        public IQueryable<T> FindAll(bool trackChanges = false) =>
            trackChanges ? Context.Set<T>() : Context.Set<T>().AsNoTracking();

        public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false) =>
            trackChanges
                ? Context.Set<T>().Where(expression)
                : Context.Set<T>().Where(expression).AsNoTracking();

        public void Create(T entity) => Context.Set<T>().Add(entity);
        public void Update(T entity) => Context.Set<T>().Update(entity);
        public void Delete(T entity) => Context.Set<T>().Remove(entity);
    }
}
