# Anexo A v3 — Especificación narrativa de los módulos de función RFC

**Para:** Carlos Argenis (ABAP) · **De:** Portal Gerencial SAP PM (CAC) · **Fecha:** 03/10/2026
**Reemplaza** a la tabla de campos del Anexo A v2: lo que cambia es que cada módulo dice **para qué
lo usa el portal, qué universo debe traer y qué NO debe filtrar**. Los campos sugeridos son una
guía; si conoces una tabla o campo mejor para el mismo dato, usa el tuyo y anótalo.

---

## 0. Contexto: qué hace el portal con estos datos

El portal reemplaza dos reportes de Oracle Forms del Centro de Servicios:

1. **Disponibilidad de Equipos:** por cada grupo de equipos (p. ej. "Bombas de Aguacatico") y a
   cada hora del día, cuántos equipos están **disponibles** y cuántos están **fuera de servicio**
   porque tienen una orden de mantenimiento abierta. Un equipo sale de servicio cuando **se crea**
   su orden y vuelve cuando la orden tiene **cierre técnico**. En las correctivas manda el **aviso**
   (si indica parada, desde/hasta la avería).
2. **Avance de Reparación:** del programa anual de reparación (órdenes ZC03), qué porcentaje de las
   **operaciones** está terminado cada semana, comparado con lo planificado.

**Cómo se usan los módulos:** un proceso del portal los llama **una vez al día (05:00)**, solo
lectura. Cada módulo que recibe fecha devuelve **lo que cambió desde esa fecha** (el portal pasa la
fecha de su última extracción buena menos un día). La primera vez pasa **01.06.2026** (salida en vivo
de SAP PM). El portal guarda todo en sus propias tablas en Oracle; nunca escribe en SAP.

**Cruce con Oracle:** el portal une SAP con Oracle por la **Ficha** del equipo (p. ej. `F01168`),
que en SAP está en el número de flota (**FLEET_NUM**). Sin Ficha, el equipo no se puede cruzar.

**Reglas comunes a todos los módulos**

- Remote-Enabled, sin `COMMIT`, sin escribir nada.
- **No filtrar por más de lo que pide cada módulo.** Las tablas que solo dan descripciones (textos,
  centros, puestos de trabajo) deben ir como **LEFT JOIN / lectura opcional**: si falta la
  descripción, la fila igual sale.
- Fechas en `DATS`, horas en `TIMS`; fecha vacía = `00000000` (el portal la trata como vacía).
- Números de equipo, orden y aviso como vienen en SAP (con o sin ceros: el portal los normaliza).
- Si el módulo lee de una tabla Z intermedia (`ZT_*`), esa tabla tiene que estar actualizada
  **antes de las 05:00**. Si no, mejor leer las tablas estándar directamente.

---

## 1. `Z_PM_MF_WS_MAESTRO_EQUIPO` — Maestro de equipos (entregado)

**Para qué lo usa el portal.** Es el diccionario que traduce un equipo de SAP (EQUNR) a la Ficha de
Oracle. Con eso el portal sabe a qué grupo de disponibilidad y a qué familia del programa de
reparación pertenece cada orden. También dice si el equipo está inactivo (no cuenta en la
disponibilidad).

**Universo.** **Todos** los equipos de los centros de mantenimiento de CAC, **tengan o no
órdenes**, activos e inactivos. Es un maestro: no se relaciona con órdenes ni avisos. Si un equipo
no tiene datos de flota, igual debe salir (con la Ficha vacía): el portal lo reporta como "equipo sin
clasificar" para que se corrija.

**Entrada.** Ninguna (siempre el maestro completo; son pocos miles de registros).

**Tablas sugeridas.** `EQUI` (base) → `EQKT` (texto, LEFT) → `EQUZ`/`ILOA` vigentes (centro, puesto
de trabajo, ubicación técnica; LEFT) → `FLEET` (número de flota, LEFT) → estado de sistema del equipo
(`JEST` con `EQUI-OBJNR`, o el texto de `STATUS_TEXT_EDIT`).

| Campo | Para qué | Obligatorio |
|---|---|---|
| EQUNR | Llave del equipo | Sí |
| EQKTX | Descripción en las pantallas | Sí |
| KEY_NUM (o FLEET_NUM) | **Ficha de Oracle**: el cruce. Confirmar que sale de FLEET_NUM | Sí (vacío si no tiene) |
| ESTADO_EQUIPO | Saber si está inactivo (INAC) o con petición de borrado (PTBO) | Sí |
| IWERK, SWERK, WORKCENTER, TPLNR | Contexto (filtros futuros) | No |

**Casos a cuidar.** Equipos sin combustible no tienen datos de flota: deben salir igual. Si un
equipo tuviera la Ficha en otro campo (EQUI-INVNR, EQUI-TIDNR, característica), decirlo.

---

## 2. `Z_PM_MF_WS_AVISO_MANTENIMIENTO` — Avisos (entregado, completo)

**Para qué lo usa el portal.** Solo para las **correctivas**: el aviso dice si el equipo quedó
**parado** (MSAUS) y desde/hasta cuándo (inicio y fin de la avería). Si el aviso dice que no hubo
parada, la correctiva no saca el equipo de servicio.

**Universo.** Avisos de mantenimiento creados o modificados desde la fecha de entrada, de todas
las clases. No hace falta que tengan orden.

**Entrada.** `IV_FECHA_DESDE`: avisos con QMEL-ERDAT ≥ fecha **o** AEDAT ≥ fecha (un aviso viejo al
que le ponen fin de avería hoy debe volver a salir).

Campos: los que ya entrega (QMNUM, QMART, QMTXT, EQUNR, AUFNR, QMDAT/MZEIT, **MSAUS**,
**AUSVN/AUZTV**, **AUSBS/AUZTB**, AUSZT, estados, QMDAB, AEDAT). **Pendiente solo confirmar la
entrada por fecha** y la frecuencia de carga de `ZT_AVISOS_MANTENIMIENTO`.

---

## 3. Órdenes de mantenimiento (pendiente) — nombre sugerido `Z_PM_MF_WS_ORDEN_MANTENIMIENTO`

**Para qué lo usa el portal.** Es el dato central de los dos reportes:
- **Disponibilidad:** cada orden abierta de las clases correctiva y preventiva saca a su equipo de
  servicio **desde la fecha y hora de creación** hasta la **fecha y hora del cierre técnico**. Por
  eso la **hora** de creación y de cierre importa (el reporte mide hora por hora).
- **Avance:** las órdenes ZC03 del programa de reparación y cuándo se cierran.

**Universo.** Órdenes de mantenimiento (AUFK-AUTYP = 30) de **todas las clases** (ZC01, ZC02,
ZC03...) que **cambiaron desde la fecha de entrada por cualquier vía**: AUFK-AEDAT ≥ fecha, **o**
cambio de estado en JCDS (UDATE ≥ fecha), **o** notificación nueva en AFRU (ERSDA ≥ fecha). Un
cierre técnico no siempre cambia AEDAT: por eso las tres condiciones. **Incluir** las anuladas o con
petición de borrado (el portal las descarta, pero necesita saber que lo están). **No** limitar a
órdenes abiertas.

**Entrada.** `IV_FECHA_DESDE`.

**Tablas sugeridas.** `AUFK` (base) → `AFIH` (equipo, clase de actividad PM, aviso) → `AFKO` (fechas
plan y reales con hora) → `ILOA` (ubicación técnica, LEFT) → estados (`STATUS_TEXT_EDIT`) → `JCDS`
para la fecha y hora en que la orden entró en **CTEC** (I0045) por primera vez sin revertirse.

| Campo | Para qué | Obligatorio |
|---|---|---|
| AUFNR | Llave | Sí |
| AUART | Clase: decide si cuenta para disponibilidad (ZC01, ZC02) o avance (ZC03) | Sí |
| ILART | Clase de actividad PM (separa horas planificada / preventiva / correctiva en el detalle) | Sí |
| EQUNR | Equipo: el cruce con el maestro | Sí (vacío si es sobre ubicación técnica) |
| KTEXT | Texto de la orden en el detalle | Sí |
| VAPLZ | Puesto de trabajo responsable | No |
| QMNUM | Aviso que la originó (para la regla de parada) | Sí si existe |
| ERDAT + **ERFZEIT** | **Inicio de la indisponibilidad** | Sí |
| GSTRP/GSUZP, GLTRP/GLUZP | Fechas plan | Sí |
| GSTRI/GSUZI, GLTRI/GEUZI | Fechas reales | Sí si existen |
| **FECHA_TECO + HORA_TECO** | **Fin de la indisponibilidad** (de JCDS; IDAT2 no trae hora) | Sí si está en CTEC |
| STATUS_SISTEMA, STATUS_USUARIO | Texto de estados ("LIB. NOTP", "CTEC"...) | Sí |
| AEDAT | Control | No |

---

## 4. Operaciones de las órdenes (pendiente) — nombre sugerido `Z_PM_MF_WS_OPERACION_ORDEN`

**Para qué lo usa el portal.** El **Avance de Reparación** se mide por operaciones (actividades):
una operación está **terminada** cuando tiene horas reales notificadas, y cuenta en la **semana en
que se digitó** esa notificación. Los "días" de cada operación salen de sus fechas plan. Además el
portal separa las operaciones de la **hoja de ruta** (creadas por el usuario de planificación) de las
**agregadas** durante la reparación (creadas por otro usuario).

**Universo.** **Todas** las operaciones de las órdenes que devolvería el módulo 3 con la misma fecha,
**más** las operaciones de cualquier orden con notificaciones nuevas desde esa fecha. Una fila por
operación (no por notificación). Incluir operaciones **sin** notificar (son las pendientes).

**Entrada.** `IV_FECHA_DESDE`.

**Tablas sugeridas.** `AFKO-AUFPL` → `AFVC` (operación, texto, puesto de trabajo) → `AFVV` (trabajo,
fechas plan) → `AFRU` agregada por operación, **excluyendo las notificaciones anuladas** (STOKZ = 'X') y
las de anulación (STZHL ≠ 0).

| Campo | Para qué | Obligatorio |
|---|---|---|
| AUFNR, VORNR | Llave | Sí |
| LTXA1 | Texto de la actividad | Sí |
| ARBPL | Puesto de trabajo | No |
| ARBEI / ARBEH | Trabajo planificado y unidad | Sí |
| FSAVD/FSAVZ, FSEDD/FSEDZ | Inicio y fin plan: los "días" de la actividad | Sí |
| ISMNW_TOTAL / ISMNE | Horas reales (suma de notificaciones válidas) | Sí |
| FECHA_PRIMERA_CONF, **FECHA_ULTIMA_CONF** | Semana en que cuenta: **fecha en que se digitó** (AFRU-ERSDA) | Sí |
| CONF_FINAL | 'X' si alguna notificación es final | Sí |
| **ERNAM (usuario creador)** | Hoja de ruta (usuario de planificación) vs. agregada | Sí |
| SISTEMA / SUBSISTEMA / COMPONENTE | Agrupación del análisis (Potencia, Tracción...) | Si existe en SAP |

---

## 5. Historial de estados de las órdenes (pendiente) — nombre sugerido `Z_PM_MF_WS_HISTORIAL_ESTADO`

**Para qué lo usa el portal.** Reconstruir con exactitud cuándo cambió de estado cada orden (sobre
todo la hora de creación y del cierre técnico, y si se reabrió). Si el módulo 3 ya trae FECHA_TECO y
HORA_TECO bien calculadas, este módulo es un respaldo y puede esperar.

**Universo.** Cambios de estado de **órdenes** (JCDS con OBJNR que empieza por "OR") con
UDATE ≥ fecha de entrada. Estados de sistema y de usuario.

| Campo | Para qué |
|---|---|
| AUFNR (de OBJNR) | Orden |
| STAT, TXT04 | Estado (I0045 = CTEC...) |
| CHGNR | Número de cambio (llave) |
| UDATE, UTIME | Fecha y hora del cambio |
| CHIND | I = alta / U = cambio |
| INACT | Si el estado quedó inactivo (se revirtió) |

---

## 5b. Agregado el 06/10/2026 — reporte Tiempo de Permanencia

- **Maestro de equipos:** además de lo de la sección 1, el portal usa **EQART / EQART_DESC** (tipo
  de equipo = familia del reporte), **INGRP / INGRP_DESC** (grupo de planificación: CDS = Centro de
  Servicio, IND = Industrial) y **TPLNR / TPLNR_DESC** (ubicación técnica). Ya vienen en
  `Z_PM_MF_WS_MAESTRO_EQUIPO`: no hace falta cambiarlo.
- **Historial de estados (módulo 5):** pasa a ser necesario (ya no es respaldo). Debe traer también
  los **estados de usuario** (E0001 PEND, E0002 ENPR, E0003 FINL) y la marca **INACT**: con ella se
  mide cuánto estuvo la orden en cada estado. Si se crea un estado de usuario "en espera de
  repuestos", se configura en el portal sin cambiar nada más.
- **Avisos:** QMDAT + MZEIT son el inicio de la permanencia (ya vienen).

## 6. Preguntas para el ABAP

1. ¿KEY_NUM sale de FLEET_NUM? ¿Los equipos sin flota tienen la Ficha en algún otro campo?
2. ¿Los módulos leen de tablas `ZT_*`? ¿A qué hora se llenan?
3. ¿Qué valores trae ESTADO_EQUIPO?
4. ¿Dónde viven Sistema / Subsistema / Componente de las operaciones?
5. Retención de JCDS desde el 01/06/2026.

**Cómo probar cada módulo.** En SE37 con `IV_FECHA_DESDE = 01.07.2026`, bajar el resultado a Excel
y dejarlo en `sprint0\sap\`: el portal lo carga tal cual (reconoce las columnas) y se ve en las
pantallas sin esperar la conexión RFC.
