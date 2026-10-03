using System.Globalization;
using PortalGerencialBackEnd.Core.Constants;

namespace PortalGerencialBackEnd.Infrastructure.Services
{
    /// <summary>
    /// Cómo se edita cada parámetro en la pantalla Sistema → Parámetros: nombre legible, sección,
    /// valores permitidos y validación. Un parámetro que no está aquí se edita como texto libre.
    /// </summary>
    internal static class CatalogoParametros
    {
        public sealed record Definicion(string Seccion, string Nombre, string[]? Opciones = null, Func<string, string?>? Validar = null);

        private static string? Entero(string v) =>
            int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > 0 ? null : "Debe ser un número entero mayor que cero.";

        private static string? Hora(string v) =>
            TimeSpan.TryParseExact(v, @"hh\:mm", CultureInfo.InvariantCulture, out _) ? null : "Formato HH:mm (p. ej. 05:00).";

        private static string? Fecha(string v) =>
            DateTime.TryParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ? null : "Formato aaaa-mm-dd.";

        private static string? Lista(string v) =>
            v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length > 0 ? null : "Al menos un valor, separados por coma.";

        private static string? ClaseIgualTipo(string v) =>
            v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .All(par => par.Split('=') is [var clase, var tipo] && clase.Trim().Length > 0
                            && tipo.Trim().ToUpperInvariant() is TiposOrden.Planificada or TiposOrden.Preventivo or TiposOrden.Correctiva)
                ? null
                : "Pares CLASE=TIPO separados por coma; TIPO es CORRECTIVA, PREVENTIVO o PLANIFICADA.";

        public static readonly IReadOnlyDictionary<string, Definicion> Definiciones = new Dictionary<string, Definicion>
        {
            [ClavesParametro.BasePorcentajeDisponibilidad] = new("Disponibilidad", "Base del % de disponibilidad", new[] { "REQUERIDOS", "EQUIPOS" }),
            [ClavesParametro.ClasesOrdenDisponibilidad] = new("Disponibilidad", "Clases de orden que sacan el equipo de servicio", Validar: Lista),
            [ClavesParametro.InicioVentana] = new("Disponibilidad", "Desde cuándo cuenta fuera de servicio", new[] { "CREACION", "INICIO_REAL" }),
            [ClavesParametro.CorrectivasSegunAviso] = new("Disponibilidad", "Correctivas según la parada del aviso", new[] { "S", "N" }),
            [ClavesParametro.ClasesOrdenAvance] = new("Avance", "Clases de orden del programa de reparación", Validar: Lista),
            [ClavesParametro.BasePlanAvance] = new("Avance", "Base del % programado", new[] { "DIAS", "ACTIVIDADES" }),
            [ClavesParametro.UsuariosHojaRuta] = new("Avance", "Usuarios SAP que crean la hoja de ruta", Validar: Lista),
            [ClavesParametro.TipoPorClaseOrden] = new("SAP", "Tipo de horas por clase de orden", Validar: ClaseIgualTipo),
            [ClavesParametro.EstadosCerrados] = new("SAP", "Estados que cierran la orden", Validar: Lista),
            [ClavesParametro.EstadosExcluidos] = new("SAP", "Estados que no cuentan", Validar: Lista),
            [ClavesParametro.FechaCargaInicial] = new("SAP", "Fecha de la primera extracción", Validar: Fecha),
            [ClavesParametro.HoraCorridaDiaria] = new("Actualización", "Hora de la corrida diaria", Validar: Hora),
            [ClavesParametro.DiasRecalculoDisponibilidad] = new("Actualización", "Días que se recalculan en cada corrida", Validar: Entero),
            [ClavesParametro.HorasAlertaFrescura] = new("Actualización", "Horas sin actualizar para alertar", Validar: Entero)
        };

        /// <summary>Valor normalizado, o el motivo por el que no es válido.</summary>
        public static (string? Valor, string? Error) Validar(string clave, string? valor)
        {
            var v = (valor ?? "").Trim();
            if (v.Length == 0)
                return (null, "El valor no puede quedar vacío.");
            if (v.Length > 500)
                return (null, "Máximo 500 caracteres.");
            if (!Definiciones.TryGetValue(clave, out var d))
                return (v, null);
            if (d.Opciones != null)
            {
                var opcion = d.Opciones.FirstOrDefault(o => o.Equals(v, StringComparison.OrdinalIgnoreCase));
                return opcion != null ? (opcion, null) : (null, $"Valores permitidos: {string.Join(", ", d.Opciones)}.");
            }
            var error = d.Validar?.Invoke(v);
            return error == null ? (v, null) : (null, error);
        }
    }
}
