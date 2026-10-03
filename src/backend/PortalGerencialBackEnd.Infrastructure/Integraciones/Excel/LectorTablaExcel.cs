using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace PortalGerencialBackEnd.Infrastructure.Integraciones.Excel
{
    /// <summary>Una hoja (o archivo de texto) leída como tabla: encabezados normalizados y filas.</summary>
    internal sealed class TablaExcel
    {
        public string Origen { get; init; } = default!;
        public HashSet<string> Encabezados { get; init; } = default!;
        public List<FilaExcel> Filas { get; init; } = default!;

        public bool Tiene(params string[] alias) => alias.Any(a => Encabezados.Contains(LectorTablaExcel.Normalizar(a)));
    }

    /// <summary>
    /// Fila de un export de SAP. Cada dato se pide por varios nombres posibles (el técnico, como
    /// AUFNR, y las etiquetas en español que pone SAP según la transacción y el layout).
    /// </summary>
    internal sealed class FilaExcel
    {
        private static readonly string[] FormatosFecha =
        {
            "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "yyyyMMdd",
            "dd.MM.yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy H:mm", "yyyy-MM-ddTHH:mm:ss"
        };

        private static readonly string[] FormatosHora = { @"hh\:mm\:ss", @"h\:mm\:ss", @"hh\:mm", @"h\:mm", "hhmmss" };

        private readonly Dictionary<string, object?> _valores;

        public FilaExcel(Dictionary<string, object?> valores)
        {
            _valores = valores;
        }

        public object? Valor(params string[] alias)
        {
            foreach (var a in alias)
                if (_valores.TryGetValue(LectorTablaExcel.Normalizar(a), out var v) && v is not null && !(v is string s && string.IsNullOrWhiteSpace(s)))
                    return v;
            return null;
        }

        public string? Texto(params string[] alias) => Valor(alias) switch
        {
            null => null,
            string s => s.Trim(),
            double d => d.ToString(CultureInfo.InvariantCulture),
            DateTime f => f.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            var v => v.ToString()?.Trim()
        };

        public DateTime? Fecha(params string[] alias)
        {
            switch (Valor(alias))
            {
                case DateTime f: return f.Year < 1901 ? null : f;
                case double d when d > 1: return DateTime.FromOADate(d);
                case string s:
                    var t = s.Trim();
                    if (t.Trim('0', '.', '-', '/', ' ').Length == 0) return null;   // 00.00.0000 = vacío en SAP
                    return DateTime.TryParseExact(t, FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var r) ? r : null;
                default: return null;
            }
        }

        public TimeSpan? Hora(params string[] alias)
        {
            switch (Valor(alias))
            {
                case TimeSpan h: return h;
                case DateTime f: return f.TimeOfDay;
                case double d when d is >= 0 and < 1: return TimeSpan.FromDays(d);
                case string s:
                    return TimeSpan.TryParseExact(s.Trim(), FormatosHora, CultureInfo.InvariantCulture, out var r) ? r : null;
                default: return null;
            }
        }

        /// <summary>Fecha + hora; si no viene la hora, la que se indique (el importador lo avisa).</summary>
        public DateTime? FechaHora(string[] fecha, string[] hora, TimeSpan siNoHayHora) =>
            Fecha(fecha) is { } f ? f.Date + (f.TimeOfDay != TimeSpan.Zero ? f.TimeOfDay : Hora(hora) ?? siNoHayHora) : null;

        public decimal Numero(params string[] alias) => Valor(alias) switch
        {
            double d => (decimal)d,
            string s => ParsearNumero(s),
            _ => 0m
        };

        /// <summary>Marca de SAP: "X" (o Sí/S/1) = verdadero.</summary>
        public bool Marca(params string[] alias) => Valor(alias) switch
        {
            bool b => b,
            double d => d != 0,
            string s => s.Trim().ToUpperInvariant() is "X" or "S" or "SI" or "SÍ" or "1" or "TRUE" or "YES" or "VERDADERO",
            _ => false
        };

        /// <summary>"1.234,50" (es), "1,234.50" (en), "8,5" o "8.5".</summary>
        public static decimal ParsearNumero(string texto)
        {
            var s = texto.Trim().Replace(" ", "");
            var negativo = s.EndsWith('-');   // SAP pone el signo al final: "12,5-"
            s = s.TrimEnd('-');
            var coma = s.LastIndexOf(',');
            var punto = s.LastIndexOf('.');
            if (coma >= 0 && punto >= 0)
                s = coma > punto ? s.Replace(".", "").Replace(',', '.') : s.Replace(",", "");
            else if (coma >= 0)
                s = s.Replace(',', '.');
            var ok = decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var n);
            return ok ? (negativo ? -n : n) : 0m;
        }
    }

    /// <summary>
    /// Lee exports de SAP tal como los baja el usuario (.xlsx de "Hoja de cálculo", o texto
    /// separado por tabuladores / punto y coma de "Fichero local"). El encabezado no siempre está en
    /// la primera fila: se toma la fila, entre las primeras 15, con más nombres de columna conocidos.
    /// </summary>
    internal static class LectorTablaExcel
    {
        public static string Normalizar(string texto)
        {
            var sb = new StringBuilder();
            foreach (var c in texto.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        public static List<TablaExcel> Leer(string ruta, ISet<string> encabezadosConocidos)
        {
            var extension = Path.GetExtension(ruta).ToLowerInvariant();
            var hojas = extension is ".xlsx" or ".xlsm"
                ? LeerXlsx(ruta)
                : new List<(string, List<object?[]>)> { (Path.GetFileName(ruta), LeerTexto(ruta)) };

            var tablas = new List<TablaExcel>();
            foreach (var (origen, filas) in hojas)
            {
                var tabla = ArmarTabla(origen, filas, encabezadosConocidos);
                if (tabla != null) tablas.Add(tabla);
            }
            return tablas;
        }

        private static List<(string, List<object?[]>)> LeerXlsx(string ruta)
        {
            var resultado = new List<(string, List<object?[]>)>();
            using var libro = new XLWorkbook(ruta);
            foreach (var hoja in libro.Worksheets)
            {
                var rango = hoja.RangeUsed();
                if (rango == null) continue;
                var columnas = rango.LastColumn().ColumnNumber();
                var filas = new List<object?[]>();
                foreach (var fila in rango.Rows())
                {
                    var valores = new object?[columnas];
                    for (var c = 1; c <= columnas; c++)
                        valores[c - 1] = Convertir(fila.WorksheetRow().Cell(c).Value);
                    filas.Add(valores);
                }
                resultado.Add(($"{Path.GetFileName(ruta)} / {hoja.Name}", filas));
            }
            return resultado;
        }

        private static object? Convertir(XLCellValue v) => v.Type switch
        {
            XLDataType.Blank => null,
            XLDataType.Boolean => v.GetBoolean(),
            XLDataType.Number => v.GetNumber(),
            XLDataType.Text => v.GetText(),
            XLDataType.DateTime => v.GetDateTime(),
            XLDataType.TimeSpan => v.GetTimeSpan(),
            _ => null
        };

        private static List<object?[]> LeerTexto(string ruta)
        {
            var lineas = File.ReadAllLines(ruta, DetectarCodificacion(ruta))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();
            if (lineas.Count == 0) return new List<object?[]>();
            var muestra = string.Join('\n', lineas.Take(15));
            var separador = new[] { '\t', ';', '|', ',' }.OrderByDescending(c => muestra.Count(x => x == c)).First();
            return lineas.Select(l => l.Split(separador).Select(x => (object?)x.Trim().Trim('"')).ToArray()).ToList();
        }

        private static Encoding DetectarCodificacion(string ruta)
        {
            var bytes = File.ReadAllBytes(ruta);
            try
            {
                new UTF8Encoding(false, true).GetString(bytes);
                return Encoding.UTF8;
            }
            catch (DecoderFallbackException)
            {
                return Encoding.Latin1;   // SAP GUI baja "Fichero local" en la página de códigos de Windows
            }
        }

        private static TablaExcel? ArmarTabla(string origen, List<object?[]> filas, ISet<string> conocidos)
        {
            var mejor = -1;
            var mejorPuntaje = 0;
            for (var i = 0; i < Math.Min(15, filas.Count); i++)
            {
                var puntaje = filas[i].Count(c => c is string s && conocidos.Contains(Normalizar(s)));
                if (puntaje > mejorPuntaje)
                {
                    mejor = i;
                    mejorPuntaje = puntaje;
                }
            }
            if (mejor < 0 || mejorPuntaje < 2) return null;

            var encabezados = filas[mejor].Select(c => c is string s ? Normalizar(s) : "").ToArray();
            var datos = new List<FilaExcel>();
            foreach (var fila in filas.Skip(mejor + 1))
            {
                if (fila.All(c => c is null || (c is string s && string.IsNullOrWhiteSpace(s)))) continue;
                var valores = new Dictionary<string, object?>();
                for (var c = 0; c < encabezados.Length && c < fila.Length; c++)
                    if (encabezados[c].Length > 0 && !valores.ContainsKey(encabezados[c]))
                        valores[encabezados[c]] = fila[c];
                datos.Add(new FilaExcel(valores));
            }
            return new TablaExcel
            {
                Origen = origen,
                Encabezados = encabezados.Where(e => e.Length > 0).ToHashSet(),
                Filas = datos
            };
        }
    }
}
