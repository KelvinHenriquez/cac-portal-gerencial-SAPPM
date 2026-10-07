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
    /// Cruce SAP ↔ estructura de los reportes por la <b>clave de equipo</b>: la Ficha de Oracle
    /// (FLEET_NUM) si el equipo la tiene y, si no, su código de equipo de SAP (EQUNR). Los equipos de
    /// fábrica nunca tendrán Ficha: con el código SAP igual se calculan, y una estructura configurada
    /// por código SAP (cuando deje de vivir en Oracle) funciona sin cambios. Ver docs/IDENTIDAD_EQUIPOS.md.
    /// Lo usan el job y los drill-down de las pantallas, para que los dos lleguen al mismo número.
    /// </summary>
    internal sealed class MapeoEquipos
    {
        /// <summary>Clave de un equipo de SAP: Ficha si la tiene; si no, el código de equipo SAP.</summary>
        public static string Clave(string? codigoLegado, string equipoSapId) =>
            string.IsNullOrWhiteSpace(codigoLegado) ? equipoSapId : codigoLegado.Trim();

        /// <summary>Equipo SAP → clave de equipo (todos los equipos).</summary>
        public Dictionary<string, string> ClavePorEquipoSap { get; private init; } = default!;

        /// <summary>Clave de equipo → equipo SAP.</summary>
        public Dictionary<string, string> EquipoSapPorClave { get; private init; } = default!;

        /// <summary>Claves de equipos marcados inactivos en SAP.</summary>
        public HashSet<string> InactivosEnSap { get; private init; } = default!;

        /// <summary>Clave → equipo de la estructura (familia, descripción, activo).</summary>
        public Dictionary<string, EquipoFamiliaDto> EquiposOracle { get; private init; } = default!;

        public bool CuentaParaDisponibilidad(string legado) =>
            EquiposOracle.TryGetValue(legado, out var e) && e.Activo && !InactivosEnSap.Contains(legado);

        public string? FamiliaDeEquipoSap(string? equipoSapId) =>
            equipoSapId != null && ClavePorEquipoSap.TryGetValue(equipoSapId, out var legado)
                && EquiposOracle.TryGetValue(legado, out var e) ? e.FamiliaCodigo : null;

        public static async Task<MapeoEquipos> CargarAsync(PortalGerencialDbContext db, IStructureSource estructura)
        {
            var oracle = await estructura.GetEquiposFamiliaAsync();
            var sap = (await db.StgSapEquipos.AsNoTracking()
                    .Select(e => new { e.EquipoSapId, e.CodigoLegado, e.Inactivo })
                    .ToListAsync())
                .Select(e => new { e.EquipoSapId, Clave = Clave(e.CodigoLegado, e.EquipoSapId), e.Inactivo })
                .ToList();

            return new MapeoEquipos
            {
                ClavePorEquipoSap = sap.ToDictionary(e => e.EquipoSapId, e => e.Clave),
                // Con Fichas repetidas en SAP se queda el equipo activo.
                EquipoSapPorClave = sap.GroupBy(e => e.Clave, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Inactivo).First().EquipoSapId, StringComparer.OrdinalIgnoreCase),
                InactivosEnSap = sap.GroupBy(e => e.Clave, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.All(e => e.Inactivo)).Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase),
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
                TipoPorClase: tipoPorClase,
                BaseEquipos: string.Equals(p.Texto(ClavesParametro.BasePorcentajeDisponibilidad, "REQUERIDOS"), "EQUIPOS", StringComparison.OrdinalIgnoreCase),
                AvancePorFinReal: !string.Equals(p.Texto(ClavesParametro.FechaOrdenTerminadaAvance, "FIN_REAL"), "CIERRE_TECNICO", StringComparison.OrdinalIgnoreCase),
                PesoActividad: p.Texto(ClavesParametro.PesoActividadAvance, "CANTIDAD").ToUpperInvariant());
        }

        public static HashSet<string> ClasesAvance(ParametrosPortal p) => p.Lista(ClavesParametro.ClasesOrdenAvance, "ZC03");

        /// <summary>
        /// Filas que no son grupo y cuyos equipos están todos dentro de una fila de grupo del mismo
        /// reporte → nombre de ese grupo (el primero, por orden).
        /// </summary>
        public static Dictionary<int, string> MiembrosDeGrupos(IReadOnlyCollection<AsignacionDto> asignaciones)
        {
            var grupos = asignaciones.Where(a => a.Grupo)
                .Select(g => (g.Nombre, Equipos: g.Equipos.ToHashSet(StringComparer.OrdinalIgnoreCase)))
                .ToList();
            var resultado = new Dictionary<int, string>();
            foreach (var a in asignaciones.Where(a => !a.Grupo && a.Equipos.Count > 0))
            {
                var grupo = grupos.FirstOrDefault(g => a.Equipos.All(g.Equipos.Contains));
                if (grupo.Nombre != null)
                    resultado[a.AsignacionId] = grupo.Nombre;
            }
            return resultado;
        }

        /// <summary>"CDS=Centro de Servicios,IND=Industrial" → diccionario código → nombre (orden de la lista).</summary>
        public static List<(string Codigo, string Nombre)> Pares(ParametrosPortal p, string clave, string porDefecto) =>
            p.Texto(clave, porDefecto)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(par => par.Split('=', 2, StringSplitOptions.TrimEntries))
                .Where(par => par.Length == 2)
                .Select(par => (par[0], par[1]))
                .ToList();

        public static List<(string Codigo, string Nombre)> AreasPermanencia(ParametrosPortal p) =>
            Pares(p, ClavesParametro.AreasPermanencia, "CDS=Centro de Servicios,IND=Industrial");

        public static EstadosPermanencia EstadosPermanencia(ParametrosPortal p)
        {
            var estados = Pares(p, ClavesParametro.EstadosUsuarioPermanencia, "ENPR=E0002,FINL=E0003")
                .ToDictionary(x => x.Codigo, x => x.Nombre, StringComparer.OrdinalIgnoreCase);
            var espera = p.Texto(ClavesParametro.EstadoEsperaRepuestos, "-");
            return new EstadosPermanencia(estados.GetValueOrDefault("ENPR"), estados.GetValueOrDefault("FINL"), espera == "-" ? null : espera);
        }

        public static HashSet<string> UsuariosHojaRuta(ParametrosPortal p)
        {
            var usuarios = p.Lista(ClavesParametro.UsuariosHojaRuta, "-");
            usuarios.Remove("-");
            return usuarios;
        }

        /// <summary>
        /// Marca cada operación como del plan original o agregada según quién la creó: las de la
        /// hoja de ruta las crea el usuario de planificación. Sin usuarios configurados, o si SAP no
        /// trajo el creador, queda la marca que vino de SAP. Se aplica al leer (entidades sin
        /// seguimiento), así que cambiar el parámetro vale también para lo ya extraído.
        /// </summary>
        public static void AplicarHojaRuta(IEnumerable<StgSapOrden> ordenes, ISet<string> usuariosHojaRuta)
        {
            if (usuariosHojaRuta.Count == 0) return;
            foreach (var op in ordenes.SelectMany(o => o.Operaciones))
                if (!string.IsNullOrWhiteSpace(op.UsuarioCreacion))
                    op.Programada = usuariosHojaRuta.Contains(op.UsuarioCreacion.Trim());
        }

        public static BasePlan BasePlan(ParametrosPortal p) =>
            string.Equals(p.Texto(ClavesParametro.BasePlanAvance, "ACTIVIDADES"), "DIAS", StringComparison.OrdinalIgnoreCase)
                ? Core.Calculo.BasePlan.Dias
                : Core.Calculo.BasePlan.Actividades;

        /// <summary>
        /// Plan de una familia sobre el calendario COMPLETO del programa (todas las semanas que tiene
        /// cualquier familia del reporte), con 0 donde la familia no planificó nada. Así lo que se
        /// cierra o termina después del plan de la familia cae en la semana en que de verdad ocurrió,
        /// no en la última semana de su plan (06/10/2026: cierres de septiembre aparecían en agosto).
        /// </summary>
        public static List<SemanaPlanDto> PlanEnCalendario(ConfigAvanceDto reporte, FamiliaAvanceDto familia)
        {
            var propias = familia.Semanas.ToDictionary(s => s.NoSemana);
            return reporte.Familias.SelectMany(f => f.Semanas)
                .GroupBy(s => s.NoSemana)
                .Select(g => propias.TryGetValue(g.Key, out var p)
                    ? p
                    : new SemanaPlanDto { NoSemana = g.Key, Mes = g.First().Mes, FechaInicio = g.First().FechaInicio, FechaFin = g.First().FechaFin })
                .OrderBy(s => s.NoSemana)
                .ToList();
        }

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
            return o.EquipoSapId != null && m.ClavePorEquipoSap.TryGetValue(o.EquipoSapId, out var legado)
                   && equiposPrograma.Contains(legado)
                   && !reglas.EstadosExcluidos.Contains(o.EstadoOrden)
                   && referencia >= reporte.FechaInicio.AddDays(-30)
                   && referencia < reporte.FechaFin.AddDays(1);
        }

        /// <summary>
        /// Avance "por órdenes": fecha en que una orden cerrada cuenta como terminada; null si no está
        /// cerrada. Por defecto su fin real (cuando terminó el trabajo), no la fecha del cierre técnico,
        /// que el taller a veces hace en lote días después (06/10/2026: 32 órdenes cerradas el 25/08).
        /// </summary>
        public static DateTime? OrdenTerminada(StgSapOrden o, ReglasVentana reglas) =>
            !reglas.EstadosCerrados.Contains(o.EstadoOrden) ? null
            : reglas.AvancePorFinReal ? o.FechaFinReal ?? o.FechaCierre
            : o.FechaCierre ?? o.FechaFinReal;

        /// <summary>Actividad terminada = con horas reales; cuenta en la fecha en que se confirmaron.</summary>
        public static DateTime? Terminada(StgSapOperacion op) =>
            op.TrabajoRealHh > 0 ? op.FechaUltimaConf ?? op.FechaPrimeraConf : null;

        /// <summary>Lo que vale una actividad en el avance real (parámetro AVANCE.PESO_ACTIVIDAD).</summary>
        public static decimal PesoActividad(StgSapOperacion op, ReglasVentana reglas) => reglas.PesoActividad switch
        {
            "HORAS_PLAN" => op.TrabajoPlanHh,
            "DIAS" => DiasActividad(op),
            _ => 1m
        };

        /// <summary>Días de una actividad = duración entre sus fechas plan (respuesta del 01/10/2026).</summary>
        public static decimal DiasActividad(StgSapOperacion op) =>
            op.FechaInicioPlan is { } i && op.FechaFinPlan is { } f && f > i ? Math.Round((decimal)(f - i).TotalDays, 4) : 0m;
    }
}
