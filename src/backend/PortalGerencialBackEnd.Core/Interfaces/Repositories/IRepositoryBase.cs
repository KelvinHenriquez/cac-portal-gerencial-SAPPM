using System.Linq.Expressions;

namespace PortalGerencialBackEnd.Core.Interfaces.Repositories
{
    /// <summary>
    /// CRUD genérico sobre EF Core, mismo patrón IRepositoryBase de TPresto.
    /// </summary>
    public interface IRepositoryBase<T> where T : class
    {
        IQueryable<T> FindAll(bool trackChanges = false);
        IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false);
        void Create(T entity);
        void Update(T entity);
        void Delete(T entity);
    }
}
