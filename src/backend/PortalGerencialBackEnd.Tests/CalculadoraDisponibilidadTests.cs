using PortalGerencialBackEnd.Core.Calculo;
using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Dtos.Estructura;

namespace PortalGerencialBackEnd.Tests;

public class CalculadoraDisponibilidadTests
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 17, 30, 0);
    private static readonly DateTime Dia = new(2026, 9, 30);

    private static readonly ReglasVentana Reglas = new(
        ClasesIncluidas: new HashSet<string>(),
        InicioEnCreacion: true,
        CorrectivasSegunAviso: true,
        EstadosCerrados: new HashSet<string> { "Cerrada" },
        EstadosExcluidos: new HashSet<string> { "Anulada" },
        TipoPorClase: new Dictionary<string, string>
        {
            ["ZC01"] = TiposOrden.Correctiva,
            ["ZC02"] = TiposOrden.Preventivo,
            ["ZC03"] = TiposOrden.Planificada
        });

    private static OrdenDisponibilidad Orden(string legado, string clase, string estado, DateTime creacion,
        DateTime? cierre = null, AvisoDisponibilidad? aviso = null, DateTime? inicioReal = null) =>
        new($"OT-{legado}-{creacion:HHmm}", legado, clase, estado, creacion, inicioReal, null, cierre, aviso);

    private static readonly AsignacionDto Asignacion = new()
    {
        AsignacionId = 1, Nombre = "Prueba", CantRequerida = 4, DispCompPct = 100,
        Equipos = new List<string> { "E1", "E2", "E3", "E4" }
    };

    private static ILookup<string, VentanaFueraServicio> Ventanas(params OrdenDisponibilidad[] ordenes) =>
        ordenes.Select(o => CalculadoraDisponibilidad.Ventana(o, Reglas, Ahora)).OfType<VentanaFueraServicio>().ToLookup(v => v.CodigoLegado);

    [Fact]
    public void Una_orden_creada_ya_deja_el_equipo_fuera_desde_su_creacion()
    {
        var v = CalculadoraDisponibilidad.Ventana(Orden("E1", "ZC03", "Creada", Dia.AddHours(9)), Reglas, Ahora);
        Assert.NotNull(v);
        Assert.Equal(Dia.AddHours(9), v!.Desde);
        Assert.Equal(Ahora, v.Hasta);
    }

    [Fact]
    public void Con_inicio_real_como_regla_una_orden_sin_iniciar_no_cuenta()
    {
        var reglas = Reglas with { InicioEnCreacion = false };
        Assert.Null(CalculadoraDisponibilidad.Ventana(Orden("E1", "ZC03", "Liberada", Dia), reglas, Ahora));
    }

    [Fact]
    public void Una_orden_cerrada_cuenta_hasta_su_cierre_tecnico()
    {
        var v = CalculadoraDisponibilidad.Ventana(Orden("E1", "ZC02", "Cerrada", Dia.AddHours(8), cierre: Dia.AddHours(14)), Reglas, Ahora);
        Assert.Equal(6, (v!.Hasta - v.Desde).TotalHours);
    }

    [Fact]
    public void Anuladas_y_clases_no_incluidas_no_cuentan()
    {
        Assert.Null(CalculadoraDisponibilidad.Ventana(Orden("E1", "ZC01", "Anulada", Dia), Reglas, Ahora));
        var soloCorrectivas = Reglas with { ClasesIncluidas = new HashSet<string> { "ZC01" } };
        Assert.Null(CalculadoraDisponibilidad.Ventana(Orden("E1", "ZC03", "Creada", Dia), soloCorrectivas, Ahora));
    }

    [Fact]
    public void Correctiva_cuyo_aviso_no_indica_parada_no_deja_el_equipo_fuera()
    {
        var sinParada = Orden("E1", "ZC01", "En Proceso", Dia, aviso: new AvisoDisponibilidad(false, null, null));
        Assert.Null(CalculadoraDisponibilidad.Ventana(sinParada, Reglas, Ahora));
    }

    [Fact]
    public void Correctiva_con_parada_cuenta_del_inicio_al_fin_de_la_averia()
    {
        var conParada = Orden("E1", "ZC01", "Cerrada", Dia.AddHours(10), cierre: Dia.AddDays(2),
            aviso: new AvisoDisponibilidad(true, Dia.AddHours(7), Dia.AddHours(15)));
        var v = CalculadoraDisponibilidad.Ventana(conParada, Reglas, Ahora)!;
        Assert.Equal(Dia.AddHours(7), v.Desde);
        Assert.Equal(Dia.AddHours(15), v.Hasta);
        Assert.Equal(TiposOrden.Correctiva, v.Tipo);
    }

    [Fact]
    public void El_dia_es_el_promedio_de_las_fotos_horarias()
    {
        // E1 fuera de 00:00 a 12:00: en 12 de las 24 fotos hay 3 de 4 disponibles.
        var ventanas = Ventanas(Orden("E1", "ZC02", "Cerrada", Dia, cierre: Dia.AddHours(12)));
        var r = CalculadoraDisponibilidad.CalcularDia(53, Asignacion, Asignacion.Equipos, Enumerable.Range(0, 24).ToList(), ventanas, Dia);

        Assert.Equal(24, r.HorasMedidas);
        Assert.Equal(3.5m, r.EqDisponibleProm);
        Assert.Equal(87.5m, r.DispRealPct);       // (12 × 75 + 12 × 100) / 24
        Assert.Equal(96m, r.HrsDia);              // 4 equipos × 24 h
        Assert.Equal(12m, r.HrsOtPreventivo);
        Assert.Equal(96m, r.HrsRealEfec);         // la preventiva no resta: solo la correctiva
    }

    [Fact]
    public void Solo_se_mide_en_las_horas_activas_del_reporte()
    {
        // E1 fuera de 00:00 a 06:00; si el reporte mide de 6 a 18, no se nota.
        var ventanas = Ventanas(Orden("E1", "ZC02", "Cerrada", Dia, cierre: Dia.AddHours(6)));
        var r = CalculadoraDisponibilidad.CalcularDia(52, Asignacion, Asignacion.Equipos, Enumerable.Range(6, 13).ToList(), ventanas, Dia);
        Assert.Equal(100m, r.DispRealPct);
        Assert.Equal(13, r.HorasMedidas);
    }

    [Fact]
    public void Real_efectivo_es_hrs_al_dia_menos_correctivas()
    {
        var ventanas = Ventanas(
            Orden("E1", "ZC01", "Cerrada", Dia.AddHours(2), cierre: Dia.AddHours(12)),   // 10 h correctiva (sin aviso)
            Orden("E2", "ZC03", "Cerrada", Dia, cierre: Dia.AddHours(5)));              // 5 h planificada
        var r = CalculadoraDisponibilidad.CalcularDia(53, Asignacion, Asignacion.Equipos, new[] { 12 }, ventanas, Dia);
        Assert.Equal(10m, r.HrsOtCorrectiva);
        Assert.Equal(5m, r.HrsOtPlanificada);
        Assert.Equal(86m, r.HrsRealEfec);
    }

    [Fact]
    public void Si_hay_dos_ordenes_a_la_vez_la_hora_cuenta_una_vez_con_prioridad_a_la_correctiva()
    {
        var ventanas = Ventanas(
            Orden("E1", "ZC03", "Cerrada", Dia, cierre: Dia.AddHours(10)),
            Orden("E1", "ZC01", "Cerrada", Dia.AddHours(4), cierre: Dia.AddHours(6)));
        var (plan, prev, corr) = CalculadoraDisponibilidad.HorasPorTipo(new[] { "E1" }, ventanas, Dia);
        Assert.Equal(2m, corr);
        Assert.Equal(8m, plan);
        Assert.Equal(0m, prev);
    }

    [Fact]
    public void Potencial_no_tiene_tope_y_disponibilidad_si()
    {
        // 113 disponibles para 112 requeridos, como la fila Todas de la captura de Oracle.
        Assert.Equal(100m, CalculadoraDisponibilidad.Porcentaje(113, 112));
        Assert.Equal(100.89m, CalculadoraDisponibilidad.PorcentajeSinTope(113, 112));
    }

    [Fact]
    public void Total_de_asignaciones_no_deja_que_el_sobrante_de_una_tape_la_falta_de_otra()
    {
        // 10 + 21 cubiertos de 35 requeridos = 88.57 %, no (11 + 21) / 35 = 91.43 %.
        Assert.Equal(88.57m, CalculadoraDisponibilidad.PorcentajeTotal(new[] { (11m, 10), (21m, 25) }));
    }

    [Fact]
    public void La_semana_promedia_sus_dias_y_suma_las_horas()
    {
        var ventanas = Ventanas(Orden("E1", "ZC01", "Cerrada", Dia, cierre: Dia.AddDays(1)));
        var dias = Enumerable.Range(0, 3)
            .Select(i => CalculadoraDisponibilidad.CalcularDia(53, Asignacion, Asignacion.Equipos, Enumerable.Range(0, 24).ToList(), ventanas, Dia.AddDays(i - 1)))
            .ToList();
        var semana = CalculadoraDisponibilidad.AgregarSemana(53, 1,
            new SemanaCalendarioDto { NoSemana = 18, FechaInicio = Dia.AddDays(-1), FechaFin = Dia.AddDays(5) }, dias);

        Assert.Equal(3, semana.DiasCalculados);
        Assert.Equal(75m, semana.DispRealMinPct);
        Assert.Equal(Math.Round((100m + 75m + 100m) / 3, 2), semana.DispRealPct);
        Assert.Equal(24m, semana.HrsOtCorrectiva);
    }

    [Fact]
    public void Base_del_porcentaje_requeridos_o_equipos_como_Oracle()
    {
        // 3 equipos para 2 requeridos; a las 10:00 uno está en una orden.
        var equipos = new[] { "F1", "F2", "F3" };
        var ventanas = new[] { new VentanaFueraServicio("F1", "4000001", TiposOrden.Correctiva, new DateTime(2026, 9, 1, 8, 0, 0), new DateTime(2026, 9, 1, 12, 0, 0)) }
            .ToLookup(v => v.CodigoLegado);

        var requeridos = CalculadoraDisponibilidad.FotosDelDia(equipos, 2, new[] { 10 }, ventanas, new DateTime(2026, 9, 1)).Single();
        var oracle = CalculadoraDisponibilidad.FotosDelDia(equipos, 2, new[] { 10 }, ventanas, new DateTime(2026, 9, 1), baseEquipos: true).Single();

        Assert.Equal(100m, requeridos.DispPct);   // 2 de 2 requeridos
        Assert.Equal(66.67m, oracle.DispPct);     // 2 de 3 equipos (PORCIENTO_HORA de Oracle)
        Assert.Equal(100m, oracle.PotencialPct);  // el potencial siempre es sobre requeridos

        // "Todas" como Oracle: total disponible / total de equipos.
        Assert.Equal(75m, CalculadoraDisponibilidad.PorcentajeTotal(new[] { (2m, 2, 3), (4m, 5, 5) }, baseEquipos: true));
        Assert.Equal(85.71m, CalculadoraDisponibilidad.PorcentajeTotal(new[] { (2m, 2, 3), (4m, 5, 5) }, baseEquipos: false));
    }
}
