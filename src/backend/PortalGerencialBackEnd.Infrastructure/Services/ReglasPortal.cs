using Microsoft.EntityFrameworkCore;
using PortalGerencialBackEnd.Core.Calculo;
using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Dtos.Estructura;
using PortalGerencialBackEnd.Core.Entities.Sap;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.Infrastructure.Services
{
    /// <summary>
    /// Cruce SAP ↔ Oracle por código legado. Lo usan el job y los drill-down de las pantallas, para
    /// que los dos lleguen siempre al mismo número.
    /// </summary>
    internal sealed class MapeoEquipos
    {
        /// <summary>Equipo SAP → código legado (solo los que tienen uno).</summary>
        public Dictionary<string, string> LegadoPorEquipoSap { get; private init; } = default!;

        /// <summary>Código legado → equipo SAP.</summary>
        public Dictionary<string, string> EquipoSapPorLegado { get; private init; } = default!;

        /// <summary>Códigos legados de equipos marcados inactivos en SAP.</summary>
        public HashSet<string> InactivosEnSap { get; private init; } = default!;

        /// <summary>Código legado → equipo de Oracle (familia, descripción, activo).</summary>
        public Dictionary<string, EquipoFamiliaDto> EquiposOracle { get; private init; } = default!;

        public bool CuentaParaDisponibilidad(string legado) =>
            EquiposOracle.TryGetValue(legado, out var e) && e.Activo && !InactivosEnSap.Contains(legado);

        public string? FamiliaDeEquipoSap(string? equipoSapId) =>
            equipoSapId != null && LegadoPorEquipoSap.TryGetValue(equipoSapId, out var legado)
                && EquiposOracle.TryGetValue(legado, out var e) ? e.FamiliaCodigo : null;

        public static async Task<MapeoEquipos> CargarAsync(PortalGerencialDbContext db, IStructureSource estructura)
        {
            var oracle = await estructura.GetEquiposFamiliaAsync();
            var sap = await db.StgSapEquipos.AsNoTracking()
                .Where(e => e.CodigoLegado != null)
                .Select(e => new { e.EquipoSapId, e.CodigoLegado, e.Inactivo })
                .ToListAsync();

            return new MapeoEquipos
            {
                LegadoPorEquipoSap = sap.ToDictionary(e => e.EquipoSapId, e => e.CodigoLegado!),
                EquipoSapPorLegado = sap.GroupBy(e => e.CodigoLegado!, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().EquipoSapId, StringComparer.OrdinalIgnoreCase),
                InactivosEnSap = sap.Where(e => e.Inactivo).Select(e => e.CodigoLegado!).ToHashSet(StringComparer.OrdinalIgnoreCase),
                EquiposOracle = oracle.GroupBy(e => e.CodigoLegado, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase)
            };
        }
    }

    /// <summary>Reglas de negocio leídas de PARAMETRO_PGPM.</summary>
    internal static class ReglasPortal
    {
        public static ReglasVentana Ventana(ParametrosPortal p)
        {
            var tipoPorClase = p.Texto(ClavesParametro.TipoPorClaseOrden, "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(par => par.Split('=', 2, StringSplitOptions.TrimEntries))
                .Where(par => par.Length == 2)
                .ToDictionary(par => par[0], par => par[1].ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);
            var clases = p.Lista(ClavesParametro.ClasesOrdenDisponibilidad, "*");
            clases.Remove("*");

            return new ReglasVentana(
                clases,
                InicioEnCreacion: !string.Equals(p.Texto(ClavesParametro.InicioVentana, "CREACION"), "INICIO_REAL", StringComparison.OrdinalIgnoreCase),
                CorrectivasSegunAviso: string.Equals(p.Texto(ClavesParametro.CorrectivasSegunAviso, "S"), "S", StringComparison.OrdinalIgnoreCase),
                EstadosCerrados: p.Lista(ClavesParametro.EstadosCerrados, "Cerrada"),
                EstadosExcluidos: p.Lista(ClavesParametro.EstadosExcluidos, "Anulada"),
                TipoPorClase: tipoPorClase);
        }

        public static HashSet<string> ClasesAvance(ParametrosPortal p) => p.Lista(ClavesParametro.ClasesOrdenAvance, "ZC03");

        public static BasePlan BasePlan(ParametrosPortal p) =>
            string.Equals(p.Texto(ClavesParametro.BasePlanAvance, "ACTIVIDADES"), "DIAS", StringComparison.OrdinalIgnoreCase)
                ? Core.Calculo.BasePlan.Dias
                : Core.Calculo.BasePlan.Actividades;

        /// <summary>
        /// Equipos (códigos legados) del programa de una familia en un reporte: los de las OT que
        /// Oracle le tiene asignadas. Una misma familia puede estar en varios reportes con equipos
        /// distintos. Si el reporte no tiene OT asignadas todavía, todos los equipos de la familia.
        /// </summary>
        public static HashSet<string> EquiposDelPrograma(FamiliaAvanceDto familia, MapeoEquipos m) =>
            (familia.Ordenes.Count > 0
                ? familia.Ordenes.Select(o => o.CodigoLegado)
                : m.EquiposOracle.Values.Where(e => e.FamiliaCodigo == familia.FamiliaCodigo).Select(e => e.CodigoLegado))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Orden SAP del programa de una familia: su equipo es uno de los del programa, no está
        /// excluida y fue planificada (o creada) dentro de la ventana del reporte, con un mes de
        /// margen previo. (Las fechas estimadas de la familia en Oracle no se usan: son de un día.)
        /// </summary>
        public static bool EsDelPrograma(StgSapOrden o, MapeoEquipos m, ConfigAvanceDto reporte, ISet<string> equiposPrograma, ReglasVentana reglas)
        {
            var referencia = o.FechaInicioPlan ?? o.FechaCreacion;
            return o.EquipoSapId != null && m.LegadoPorEquipoSap.TryGetValue(o.EquipoSapId, out var legado)
                   && equiposPrograma.Contains(legado)
                   && !reglas.EstadosExcluidos.Contains(o.EstadoOrden)
                   && referencia >= reporte.FechaInicio.AddDays(-30)
                   && referencia < reporte.FechaFin.AddDays(1);
        }

        /// <summary>Fecha de cierre si la orden está cerrada; si no, null.</summary>
        public static DateTime? CierreSiCerrada(StgSapOrden o, ReglasVentana reglas) =>
            reglas.EstadosCerrados.Contains(o.EstadoOrden) ? o.FechaCierre ?? o.FechaFinReal : null;

        /// <summary>Actividad terminada = con horas reales; cuenta en la fecha en que se confirmaron.</summary>
        public static DateTime? Terminada(StgSapOperacion op) =>
            op.TrabajoRealHh > 0 ? op.FechaUltimaConf ?? op.FechaPrimeraConf : null;

        /// <summary>Días de una actividad = duración entre sus fechas plan (respuesta del 01/10/2026).</summary>
        public static decimal DiasActividad(StgSapOperacion op) =>
            op.FechaInicioPlan is { } i && op.FechaFinPlan is { } f && f > i ? Math.Round((decimal)(f - i).TotalDays, 4) : 0m;
    }
}
