// Formatos de fecha y número del portal (República Dominicana). Las fechas que vienen del backend
// son "YYYY-MM-DDT00:00:00" sin zona: se leen como fecha local para que no se corran un día.

function aFecha(valor: string): Date {
  const [anio, mes, dia] = valor.slice(0, 10).split("-").map(Number);
  return new Date(anio, mes - 1, dia);
}

const dosDigitos = (n: number) => String(n).padStart(2, "0");
const fmtLarga = new Intl.DateTimeFormat("es-DO", { day: "2-digit", month: "2-digit", year: "numeric" });
const fmtDiaSemana = new Intl.DateTimeFormat("es-DO", { weekday: "short" });
const fmtPct = new Intl.NumberFormat("es-DO", { minimumFractionDigits: 1, maximumFractionDigits: 1 });

/** 21/09 (a mano: es-DO ignora "2-digit" en el formato corto y da "21/9"). */
export const fechaCorta = (valor: string) => {
  const f = aFecha(valor);
  return `${dosDigitos(f.getDate())}/${dosDigitos(f.getMonth() + 1)}`;
};

/** 21/09/2026 */
export const fechaLarga = (valor: string) => fmtLarga.format(aFecha(valor));

/** lun 21/09 */
export const fechaConDia = (valor: string) =>
  `${fmtDiaSemana.format(aFecha(valor)).replace(".", "")} ${fechaCorta(valor)}`;

const fmtFechaHora = new Intl.DateTimeFormat("es-DO", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
});

/** 01/10/2026, 18:00 (para fechas con hora, que sí traen la hora del backend). */
export const fechaHora = (valor: string) => fmtFechaHora.format(new Date(valor));

/** 80.6% */
export const porcentaje = (valor: number) => `${fmtPct.format(valor)}%`;
