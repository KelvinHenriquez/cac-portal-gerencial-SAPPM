using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Interceptors
{
    /// <summary>
    /// Si el usuario técnico de producción no es el dueño del esquema _pgpm (decisión pendiente con
    /// el DBA, sección 13), cada conexión apunta a ese esquema para que las consultas sin calificar
    /// resuelvan ahí. Sin Oracle:Esquema configurado, no se registra.
    /// </summary>
    public sealed class CurrentSchemaInterceptor : DbConnectionInterceptor
    {
        private readonly string _sql;

        public CurrentSchemaInterceptor(string esquema)
        {
            if (!Regex.IsMatch(esquema, @"^[A-Za-z][A-Za-z0-9_$#]{0,127}$"))
                throw new ArgumentException($"Nombre de esquema Oracle inválido: '{esquema}'.");
            _sql = $"ALTER SESSION SET CURRENT_SCHEMA = {esquema.ToUpperInvariant()}";
        }

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            using var command = connection.CreateCommand();
            command.CommandText = _sql;
            command.ExecuteNonQuery();
        }

        public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = _sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
