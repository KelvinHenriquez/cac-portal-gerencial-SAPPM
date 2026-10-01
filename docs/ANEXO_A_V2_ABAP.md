# Anexo A v2 — Módulos RFC para el Portal Gerencial SAP PM (para el ABAP)

**Fecha:** 01/10/2026. Reemplaza al Anexo A de la especificación original.

**Qué cambió y por qué:** al revisar los reportes actuales de Oracle (Disponibilidad y Avance de
Órdenes de Trabajo) con el área, salió que:

1. **Disponibilidad:** un equipo cuenta fuera de servicio **desde que se crea la orden** (no desde el
   inicio real) hasta su cierre, y se mide **por hora**. Además las horas se separan en
   planificada / preventiva / correctiva. Para las correctivas importa el **aviso** (si indica parada
   del equipo).
2. **Avance:** se mide por **actividades (operaciones)** de las órdenes del programa de reparación:
   una operación está terminada cuando tiene horas reales, y cuenta en la **semana en que se
   confirmaron** esas horas. Los "días" de cada operación salen de sus fechas plan inicio/fin.

Todos los módulos: **Remote-Enabled** (SE37), solo lectura, sin `COMMIT`. Fechas como `SY-DATUM`,
horas como `SY-UZEIT`. Excluir siempre confirmaciones anuladas (`STOKZ = 'X'` o con `STZHL`).

---

## A.1 `Z_PM_TALLERES_EQUIPOS` — maestro de equipos (sin cambios de fondo)

Sin parámetros de entrada (trae todos los equipos de los centros de mantenimiento del área).

| Campo salida | Origen | Nota |
|---|---|---|
| EQUNR | EQUI-EQUNR | |
| EQKTX | EQKT-EQKTX | descripción |
| CODIGO_LEGADO | **¿?** | **Pendiente: en qué campo se cargó el código viejo de Oracle (Ficha, p. ej. F01118 / B01071).** Suele ser EQUI-TIDNR, EQUI-INVNR o una característica (AUSP/CABN). |
| EQART | EQUI-EQART | tipo de objeto |
| ARBPL | work center del equipo (EQUZ-GEWRK → CRHD-ARBPL) | |
| SWERK | ILOA-SWERK | centro |
| INACTIVO | 'X' si tiene status INAC (I0320) o marca de borrado | para no contarlo en disponibilidad |

## A.2 `Z_PM_TALLERES_ORDENES` — órdenes (cambia)

Entrada: `IV_FECHA_DESDE`. Devolver las órdenes de mantenimiento (AUTYP = '30') que **cambiaron
desde esa fecha por cualquier vía**: AUFK-AEDAT ≥ fecha, **o** algún cambio de status en JCDS
(UDATE ≥ fecha), **o** alguna confirmación en AFRU (ERSDA ≥ fecha). Un cierre técnico no siempre
toca AEDAT.

| Campo salida | Origen | Nota |
|---|---|---|
| AUFNR | AUFK | |
| AUART | AUFK | clase (ZC01, ZC03, ...) |
| **ILART** | AFIH-ILART | **clase de actividad PM: con esto se separan horas planificada / preventiva / correctiva** |
| KTEXT | AUFK | texto breve |
| EQUNR | AFIH | |
| TPLNR | ILOA (vía AFIH-ILOAN) | por si la orden es sobre ubicación técnica |
| VAPLZ | AUFK | puesto de trabajo responsable |
| **QMNUM** | AFIH-QMNUM | aviso que originó la orden |
| **ERDAT + ERFZEIT** | AUFK | **creación, con hora: inicio de la indisponibilidad** |
| GSTRP / GSUZP | AFKO | inicio plan + hora |
| GLTRP / GLUZP | AFKO | fin plan + hora |
| GSTRI / GSUZI | AFKO | inicio real + hora |
| GLTRI / GEUZI | AFKO | fin real + hora |
| **FECHA_TECO + HORA_TECO** | JCDS: primer `I` del status I0045 (CTEC) vigente | **fin de la indisponibilidad** (IDAT2 no trae hora) |
| STATUS_SISTEMA | STATUS_TEXT_EDIT (texto, p. ej. "LIB. CTEC") | |
| STATUS_USUARIO | STATUS_TEXT_EDIT | |

## A.3 `Z_PM_TALLERES_HISTORIAL_ESTADO` — sin cambios

JCDS de las órdenes (OBJNR LIKE 'OR%') con UDATE ≥ `IV_FECHA_DESDE`: AUFNR, STAT, TXT04, CHGNR,
UDATE, UTIME, CHIND. Confirmar la **retención de JCDS** desde el 01/06/2026.

## A.4 `Z_PM_TALLERES_OPERACIONES` — reemplaza a Z_PM_TALLERES_CONFIRMACIONES

Entrada: `IV_FECHA_DESDE`. Una fila por **operación** (actividad) de las órdenes que devolvería
A.2 con la misma fecha, más las operaciones con confirmaciones nuevas desde esa fecha.

| Campo salida | Origen | Nota |
|---|---|---|
| AUFNR, VORNR | AFKO-AUFPL → AFVC | llave |
| LTXA1 | AFVC | texto de la operación (p. ej. "Revisar") |
| ARBPL | AFVC-ARBID → CRHD | puesto de trabajo |
| ARBEI / ARBEH | AFVV | trabajo plan + unidad |
| DAUNO / DAUNE | AFVV | duración normal + unidad |
| FSAVD / FSAVZ | AFVV | inicio plan (fecha/hora) — **los "días" de la actividad salen de aquí** |
| FSEDD / FSEDZ | AFVV | fin plan (fecha/hora) |
| ISMNW_TOTAL | suma AFRU-ISMNW no anuladas | horas reales |
| FECHA_PRIMERA_CONF | min AFRU-BUDAT (o ERSDA) | |
| **FECHA_ULTIMA_CONF** | max AFRU-BUDAT | **semana en que cuenta como terminada** (confirmar si usar BUDAT o ERSDA: fecha contable vs. fecha en que se digitó) |
| CONF_FINAL | 'X' si alguna AFRU tiene AUERU = 'X' | |
| SISTEMA / SUBSISTEMA / COMPONENTE | **¿?** | **Pendiente:** en Oracle cada actividad tiene Sistema (Potencia, Tracción, Eléctrico...), Sub-sistema (Motor) y Componente (Block, Bomba de inyección...). ¿Cómo quedó eso en SAP? (texto de la operación, texto largo, lista de objetos, campos de usuario USR0x, hoja de ruta...) |

## A.5 `Z_PM_TALLERES_AVISOS` — nuevo

Entrada: `IV_FECHA_DESDE`. Avisos (QMEL/QMIH) creados o modificados desde esa fecha.

| Campo salida | Origen | Nota |
|---|---|---|
| QMNUM, QMART | QMEL | aviso y clase |
| QMTXT | QMEL | texto (motivo) |
| EQUNR | QMIH | |
| AUFNR | QMEL | orden generada |
| QMDAT / MZEIT | QMEL | fecha/hora del aviso |
| **MSAUS** | QMIH | **indicador de parada (el aviso deja el equipo fuera)** |
| **AUSVN / AUZTV** | QMIH | inicio de la avería (fecha/hora) |
| **AUSBS / AUZTB** | QMIH | fin de la avería (fecha/hora) |
| AUSZT | QMIH | duración de la parada |
| PRIOK | QMEL | prioridad |
| STATUS_SISTEMA | STATUS_TEXT_EDIT (OSNO, NOPR, NOCO...) | |
| QMDAB | QMEL | fecha de cierre |

---

## Lado SAP/Basis (sin cambios)

- Los 5 módulos marcados como Remote-Enabled.
- Usuario de comunicación con S_RFC sobre estos 5 módulos.
- Ruta de red desde el servidor del portal al gateway SAP (puerto 33xx).

## Preguntas para el ABAP

1. ¿Campo del código legado del equipo (A.1)?
2. ¿Dónde viven Sistema / Sub-sistema / Componente de las actividades (A.4)?
3. ¿Qué clases de orden (AUART) y clases de actividad PM (ILART) se usan para planificada,
   preventiva y correctiva?
4. ¿Retención de JCDS desde el 01/06/2026?
5. ¿Para la semana de la actividad terminada conviene AFRU-BUDAT (fecha contable) o ERSDA (fecha
   de captura)? El área dijo "la fecha en que se confirma o digita la hora real".
