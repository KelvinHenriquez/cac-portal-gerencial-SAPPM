namespace PortalGerencialBackEnd.Core.Interfaces.Repositories
{
    /// <summary>
    /// Agrupa todos los repositorios y expone SaveAsync — mismo patrón IRepositoryManager de TPresto.
    /// Los repositorios de las tablas _pgpm se agregan en la Etapa 2.
    /// </summary>
    public interface IRepositoryManager
    {
        Task<int> SaveAsync();

        /// <summary>
        /// Ejecuta <paramref name="action"/> dentro de una transacción y hace commit si termina sin
        /// excepción, o rollback si la lanza. La usará el job de recálculo para reemplazar los
        /// resultados de una corrida de forma atómica.
        /// </summary>
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action);
    }
}
