namespace PortalGerencialBackEnd.Infrastructure.Jobs
{
    /// <summary>
    /// Garantiza una sola corrida a la vez, venga del Background Service o del botón del portal:
    /// dos corridas simultáneas se pisarían borrando e insertando los mismos resultados.
    /// </summary>
    public sealed class CoordinadorCorridas
    {
        private readonly SemaphoreSlim _semaforo = new(1, 1);

        public bool EnCurso => _semaforo.CurrentCount == 0;

        /// <summary>Ejecuta <paramref name="accion"/> si no hay otra corrida; si la hay, devuelve (false, default).</summary>
        public async Task<(bool Ejecutada, T? Resultado)> IntentarAsync<T>(Func<Task<T>> accion)
        {
            if (!await _semaforo.WaitAsync(0))
                return (false, default);

            try
            {
                return (true, await accion());
            }
            finally
            {
                _semaforo.Release();
            }
        }
    }
}
