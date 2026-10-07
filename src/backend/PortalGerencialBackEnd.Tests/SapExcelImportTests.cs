using ClosedXML.Excel;
using PortalGerencialBackEnd.Infrastructure.Integraciones;
using PortalGerencialBackEnd.Infrastructure.Integraciones.Excel;

namespace PortalGerencialBackEnd.Tests;

/// <summary>Importador del Sprint 0 con archivos armados como los baja SAP.</summary>
public class SapExcelImportTests : IDisposable
{
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "sprint0_" + Guid.NewGuid().ToString("N"));

    public SapExcelImportTests()
    {
        Directory.CreateDirectory(_carpeta);
    }

    public void Dispose()
    {
        try { Directory.Delete(_carpeta, true); } catch (IOException) { /* temporal */ }
    }

    private void Xlsx(string nombre, string? titulo, string[] encabezados, params object?[][] filas)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Hoja1");
        var r = 1;
        if (titulo != null)
        {
            hoja.Cell(r, 1).Value = titulo;   // SAP a veces pone un título arriba del encabezado
            r += 2;
        }
        for (var c = 0; c < encabezados.Length; c++) hoja.Cell(r, c + 1).Value = encabezados[c];
        foreach (var fila in filas)
        {
            r++;
            for (var c = 0; c < fila.Length; c++)
                hoja.Cell(r, c + 1).Value = fila[c] switch
                {
                    null => Blank.Value,
                    DateTime d => d,
                    double n => n,
                    int n => n,
                    var v => v.ToString()
                };
        }
        libro.SaveAs(Path.Combine(_carpeta, nombre));
    }

    private void ArmarExports()
    {
        // Maestro desde el módulo del ABAP (SE37): nombres técnicos.
        Xlsx("maestro.xlsx", null, new[] { "EQUNR", "EQKTX", "KEY_NUM", "WORKCENTER", "ESTADO_EQUIPO" },
            new object?[] { "000000000010001234", "TRACTOR JD 6150", "F01168", "TALLER1", "" },
            new object?[] { "000000000010005555", "BOMBA ELECTRICA", null, "RIEGO", "INAC" });

        // IW39 con etiquetas en español y fechas como texto.
        Xlsx("IW39 ordenes.xlsx", "Visualizar órdenes PM: Lista", new[]
            {
                "Orden", "Clase de orden", "Clase de actividad PM", "Equipo", "Texto breve", "Status del sistema",
                "Status de usuario", "Fecha de creación", "Inicio extremo", "Fin extremo", "Fecha de inicio real",
                "Fecha de fin real", "Fecha de cierre técnico", "Aviso"
            },
            new object?[] { "4000123", "ZC01", "COR", "10001234", "Cambio de bomba", "CTEC NOTI", "", "15.07.2026", "15.07.2026", "17.07.2026", "15.07.2026", "17.07.2026", "17.07.2026", "10000077" },
            new object?[] { "4000124", "ZC03", "PRD", "10001234", "Reparación general", "LIB. NOTP", "", "01.08.2026", "03.08.2026", "30.09.2026", "03.08.2026", null, null, null },
            new object?[] { "4000125", "ZC02", "PRV", "10001234", "Preventivo 250 h", "PTBO", "", "02.08.2026", null, null, null, null, null, null });

        // Historial de estados (JCDS por SE16N): da la hora exacta de creación y de cierre técnico.
        Xlsx("jcds.xlsx", null, new[] { "OBJNR", "STAT", "CHGNR", "UDATE", "UTIME", "CHIND" },
            new object?[] { "OR000004000123", "I0001", "001", "15.07.2026", "08:12:00", "I" },
            new object?[] { "OR000004000123", "I0045", "004", "17.07.2026", "16:40:00", "I" },
            new object?[] { "IE000000000010001234", "I0320", "001", "01.01.2026", "00:00:00", "I" });

        // IW37N y IW47 de la orden ZC03.
        Xlsx("IW37N.xlsx", null, new[] { "Orden", "Operación", "Texto breve operación", "Trabajo", "Unidad de trabajo", "Trabajo real", "Fecha de inicio más temprana", "Fecha de fin más temprana", "Creado por" },
            new object?[] { "4000124", "10", "Revisar motor", "8,0", "H", "6", "03.08.2026", "05.08.2026", "PLANIFICADOR" },
            new object?[] { "4000124", "20", "Cambiar bomba inyección", "90", "MIN", "0", "06.08.2026", "06.08.2026", "JPEREZ" });
        Xlsx("IW47.xlsx", null, new[] { "Orden", "Operación", "Notificación", "Trabajo real", "Unidad", "Fecha de contabilización", "Fecha de entrada", "Notificación final", "Anulado" },
            new object?[] { "4000124", "0010", "900001", "4", "H", "04.08.2026", "05.08.2026", "", "" },
            new object?[] { "4000124", "0010", "900002", "2", "H", "06.08.2026", "07.08.2026", "X", "" },
            new object?[] { "4000124", "0010", "900003", "3", "H", "06.08.2026", "08.08.2026", "", "X" });

        // IW29.
        Xlsx("IW29.xlsx", null, new[] { "Aviso", "Clase de aviso", "Descripción", "Equipo", "Orden", "Fecha de aviso", "Hora del aviso", "Parada", "Inicio avería", "Hora inicio avería", "Fin avería", "Hora fin avería" },
            new object?[] { "10000077", "M2", "Bomba no arranca", "10001234", "4000123", "15.07.2026", "07:50:00", "X", "15.07.2026", "07:30:00", "17.07.2026", "15:00:00" });

        // Un archivo que no es de SAP: se avisa y se ignora.
        Xlsx("notas.xlsx", null, new[] { "Comentario", "Persona" }, new object?[] { "revisar", "KH" });
    }

    [Fact]
    public void Reconoce_cada_export_por_sus_columnas_y_arma_los_mismos_datos_que_el_RFC()
    {
        ArmarExports();

        var d = SapExcelExtractionClient.LeerCarpeta(_carpeta, new DateTime(2026, 10, 3));

        // Equipos: ceros a la izquierda fuera, el que no tiene ficha queda sin legado e inactivo.
        Assert.Equal(2, d.Equipos.Count);
        Assert.Equal("F01168", d.Equipos.Single(e => e.EquipoSapId == "10001234").CodigoLegado);
        var bomba = d.Equipos.Single(e => e.EquipoSapId == "10005555");
        Assert.Null(bomba.CodigoLegado);
        Assert.True(bomba.Inactivo);

        // Órdenes: estados derivados del status y horas exactas desde JCDS.
        Assert.Equal(3, d.Ordenes.Count);
        var cerrada = d.Ordenes.Single(o => o.OrdenSapId == "4000123");
        Assert.Equal(EstadosSap.Cerrada, cerrada.EstadoOrden);
        Assert.Equal(new DateTime(2026, 7, 15, 8, 12, 0), cerrada.FechaCreacion);
        Assert.Equal(new DateTime(2026, 7, 17, 16, 40, 0), cerrada.FechaCierre);
        Assert.Equal("10000077", cerrada.AvisoSapId);
        Assert.Equal("ZC01", cerrada.ClaseOrden);

        var abierta = d.Ordenes.Single(o => o.OrdenSapId == "4000124");
        Assert.Equal(EstadosSap.EnProceso, abierta.EstadoOrden);
        Assert.Null(abierta.FechaCierre);
        Assert.Equal(new DateTime(2026, 8, 1, 12, 0, 0), abierta.FechaCreacion);   // sin hora ni JCDS: mediodía
        Assert.Equal(EstadosSap.Anulada, d.Ordenes.Single(o => o.OrdenSapId == "4000125").EstadoOrden);

        // Operaciones: horas reales de IW47 sin la notificación anulada; semana = fecha de entrada.
        var op10 = d.Operaciones.Single(o => o.Operacion == "0010");
        Assert.Equal(6m, op10.TrabajoRealHh);
        Assert.Equal(new DateTime(2026, 8, 5), op10.FechaPrimeraConf);
        Assert.Equal(new DateTime(2026, 8, 7), op10.FechaUltimaConf);
        Assert.True(op10.ConfFinal);
        Assert.Equal("PLANIFICADOR", op10.UsuarioCreacion);
        Assert.Equal(8m, op10.TrabajoPlanHh);
        var op20 = d.Operaciones.Single(o => o.Operacion == "0020");
        Assert.Equal(1.5m, op20.TrabajoPlanHh);   // 90 MIN
        Assert.Equal(0m, op20.TrabajoRealHh);

        // Aviso con parada y avería con hora.
        var aviso = d.Avisos.Single();
        Assert.True(aviso.Parada);
        Assert.Equal(new DateTime(2026, 7, 15, 7, 30, 0), aviso.InicioAveria);
        Assert.Equal(new DateTime(2026, 7, 17, 15, 0, 0), aviso.FinAveria);
        Assert.Equal("4000123", aviso.OrdenSapId);

        // Historial: solo objetos de orden.
        Assert.Equal(2, d.Historial.Count);
        Assert.All(d.Historial, h => Assert.Equal("4000123", h.OrdenSapId));

        Assert.Contains(d.Mensajes, m => m.Contains("notas.xlsx") && m.Contains("no se reconoció"));
        Assert.Contains(d.Mensajes, m => m.Contains("sin código legado"));
    }

    [Fact]
    public void Lee_tambien_el_fichero_local_de_SAP_separado_por_tabuladores()
    {
        File.WriteAllText(Path.Combine(_carpeta, "iw39.txt"),
            "Orden\tClase de orden\tEquipo\tStatus del sistema\tFecha de creación\tHora de creación\n" +
            "4000200\tZC01\t10001234\tLIB.\t20.07.2026\t09:15:00\n",
            System.Text.Encoding.Latin1);

        var d = SapExcelExtractionClient.LeerCarpeta(_carpeta, new DateTime(2026, 10, 3));

        var o = d.Ordenes.Single();
        Assert.Equal(new DateTime(2026, 7, 20, 9, 15, 0), o.FechaCreacion);
        Assert.Equal(EstadosSap.Liberada, o.EstadoOrden);
    }

    [Theory]
    [InlineData("1.234,50", 1234.50)]
    [InlineData("1,234.50", 1234.50)]
    [InlineData("8,5", 8.5)]
    [InlineData("8.5", 8.5)]
    [InlineData("12,5-", -12.5)]
    public void Numeros_en_formato_de_SAP(string texto, double esperado)
    {
        Assert.Equal((decimal)esperado, FilaExcel.ParsearNumero(texto));
    }

    [Theory]
    [InlineData("CTEC NOTI", null, EstadosSap.Cerrada)]
    [InlineData("LIB. NOTP", null, EstadosSap.EnProceso)]
    [InlineData("LIB.", "EN PROCESO", EstadosSap.EnProceso)]
    [InlineData("LIB.", null, EstadosSap.Liberada)]
    [InlineData("ABIE", null, EstadosSap.Creada)]
    [InlineData("REL TECO", null, EstadosSap.Cerrada)]
    [InlineData("CTEC PTBO", null, EstadosSap.Anulada)]
    public void Estado_del_portal_desde_el_status_de_SAP(string sistema, string? usuario, string esperado)
    {
        Assert.Equal(esperado, EstadosSap.Derivar(sistema, usuario));
    }

    [Fact]
    public void Lee_las_operaciones_tal_como_las_devuelve_el_modulo_del_ABAP()
    {
        Xlsx("ordenes.xlsx", null, new[] { "AUFNR", "AUART", "EQUNR", "STATUS_SISTEMA", "ERDAT", "ERFZEIT", "FECHA_TECO", "HORA_TECO" },
            new object?[] { "000004000300", "ZC03", "10001234", "CTEC", "01.08.2026", "07:30:00", "20.08.2026", "17:05:00" });
        Xlsx("operaciones.xlsx", null, new[] { "AUFNR", "VORNR", "LTXA1", "ARBEI", "ARBEH", "ISMNW_TOTAL", "FSAVD", "FSEDD", "FECHA_PRIMERA_CONF", "FECHA_ULTIMA_CONF", "CONF_FINAL", "ERNAM" },
            new object?[] { "000004000300", "0010", "Revisar motor", "8", "H", "7,5", "03.08.2026", "05.08.2026", "04.08.2026", "06.08.2026", "X", "PLANIFICADOR" });

        var d = SapExcelExtractionClient.LeerCarpeta(_carpeta, new DateTime(2026, 10, 3));

        var orden = d.Ordenes.Single();
        Assert.Equal("4000300", orden.OrdenSapId);
        Assert.Equal(new DateTime(2026, 8, 1, 7, 30, 0), orden.FechaCreacion);
        Assert.Equal(new DateTime(2026, 8, 20, 17, 5, 0), orden.FechaCierre);
        var op = d.Operaciones.Single();
        Assert.Equal(7.5m, op.TrabajoRealHh);
        Assert.Equal(new DateTime(2026, 8, 4), op.FechaPrimeraConf);
        Assert.Equal(new DateTime(2026, 8, 6), op.FechaUltimaConf);
        Assert.True(op.ConfFinal);
        Assert.DoesNotContain(d.Mensajes, m => m.Contains("IW47"));
    }

    [Fact]
    public void Las_suboperaciones_son_las_actividades_de_la_reparacion()
    {
        Xlsx("IW39.xlsx", null, new[] { "Orden", "Clase de orden", "Equipo", "Estado sistema", "Fecha creación" },
            new object?[] { "230000000164", "ZC03", "012-040102-0051", "LIBE", "01.07.2026" });
        Xlsx("IW37N.xlsx", null, new[] { "Orden", "Operación", "Suboperación", "Txt.brv.oper.", "Trabajo", "Unidad trabajo", "Trabajo real", "FechaInicMásTmp", "FechaFinMásTmpr", "Respuesta" },
            new object?[] { "230000000164", "0010", null, "Reparación general", "0", "H", "0", "06.07.2026", "30.12.2026", null },
            new object?[] { "230000000164", "0010", "0005", "Revisar motor", "8", "H", "8", "06.07.2026", "08.07.2026", "123" },
            new object?[] { "230000000164", "0010", "0010", "Cambiar sellos", "4", "HRA", "0", "09.07.2026", "10.07.2026", null });
        Xlsx("IW47.xlsx", null, new[] { "Orden", "Operación", "Suboperación", "Trabajo real", "Un.trab.(real)", "Fecha contab.", "Creado el", "Confirm.final", "Anulado" },
            new object?[] { "230000000164", "0010", "0005", "8", "H", "08.07.2026", "09.07.2026", "X", "" });

        var d = SapExcelExtractionClient.LeerCarpeta(_carpeta, new DateTime(2026, 10, 7));

        Assert.Equal(new[] { "0010/0005", "0010/0010" }, d.Operaciones.Select(o => o.Operacion).OrderBy(o => o));
        var hecha = d.Operaciones.Single(o => o.Operacion == "0010/0005");
        Assert.Equal(8m, hecha.TrabajoRealHh);
        Assert.Equal(new DateTime(2026, 7, 9), hecha.FechaUltimaConf);   // fecha en que se digitó
        Assert.Equal(new DateTime(2026, 7, 6), hecha.FechaInicioPlan);
        Assert.Equal(new DateTime(2026, 7, 8), hecha.FechaFinPlan);
    }
}
