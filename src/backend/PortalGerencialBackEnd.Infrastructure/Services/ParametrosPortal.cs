using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.Infrastructure.Services
{
    /// <summary>Lectura tipada de PARAMETRO_PGPM, cargada una vez por uso (p. ej. por corrida).</summary>
    public sealed class ParametrosPortal
    {
        private readonly Dictionary<string, string> _valores;

        private ParametrosPortal(Dictionary<string, string> valores)
        {
            _valores = valores;
        }

        public static async Task<ParametrosPortal> CargarAsync(PortalGerencialDbContext db) =>
            new(await db.Parametros.AsNoTracking().ToDictionaryAsync(p => p.Clave, p => p.Valor));

        public string Texto(string clave, string porDefecto) =>
            _valores.TryGetValue(clave, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : porDefecto;

        public int Entero(string clave, int porDefecto) =>
            int.TryParse(Texto(clave, ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : porDefecto;

        public DateTime Fecha(string clave, DateTime porDefecto) =>
            DateTime.TryParseExact(Texto(clave, ""), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var f)
                ? f
                : porDefecto;

        public TimeSpan Hora(string clave, TimeSpan porDefecto) =>
            TimeSpan.TryParseExact(Texto(clave, ""), @"hh\:mm", CultureInfo.InvariantCulture, out var h) ? h : porDefecto;

        /// <summary>Lista separada por comas, sin distinguir mayúsculas.</summary>
        public HashSet<string> Lista(string clave, string porDefecto) =>
            Texto(clave, porDefecto)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
