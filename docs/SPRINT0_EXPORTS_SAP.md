# Sprint 0 — Validar el portal con datos reales

Objetivo (sección 6.3 de la especificación): correr el cálculo del portal con datos **reales** de
SAP y de la configuración de Oracle Forms, y compararlo contra los reportes de Oracle (que siguen
en paralelo), antes de depender del RFC.

## Cómo funciona (actualizado 03/10/2026)

El portal tiene un tercer origen de datos de SAP, `DataSourceMode=Excel`: lee los exports de una
carpeta y llena el **mismo staging** que llenará el RFC, así que el cálculo es exactamente el de
producción. Corre en una base Oracle local **aparte** (no toca la de desarrollo), con una réplica
de MAQUINARIAN cargada con la configuración real y las vistas creadas con el mismo
`docs/oracle/vistas_produccion.sql` que irá a producción.

```
docker compose -f docker-compose.dev.yml -f docker-compose.sprint0.yml up -d     # entrar al Sprint 0
docker compose -f docker-compose.dev.yml up -d                                   # volver a desarrollo
```

Los datos reales van en `C:\cac-portal-gerencial-SAPPM\sprint0\` (fuera de git):

| Carpeta | Qué va | Quién |
|---|---|---|
| `sprint0\sap\` | exports de SAP (.xlsx tal como los baja SAP, o "Fichero local" .txt) | Kelvin / SAP |
| `sprint0\oracle\` | export de la configuración de MAQUINARIAN en formato INSERT (.sql) | Kelvin (DBA) |

El importador reconoce cada archivo **por sus columnas**, no por el nombre; no hace falta renombrar
ni quitar columnas. Al correr deja en el log qué reconoció, qué faltó y qué supuso. Probado de punta
a punta el 03/10/2026 con datos sintéticos (corrida OK, pantallas de Disponibilidad y Avance).

## A. Configuración de Oracle (MAQUINARIAN) — SQL Developer

Herramientas → Exportar base de datos: **sin DDL**, datos en formato **INSERT**, un solo archivo
`sprint0\oracle\maquinarian_config.sql`. Tablas (con el filtro "WHERE" indicado):

| Tabla | Filtro |
|---|---|
| MA_MAESTRO_EQUIPOS | — (≈3.800 filas) |
| MA_CONFIG_DISPONIBILIDAD, MA_CONFIG_SEMANA_DISP, MA_CONFIG_HORA_DISP | — |
| MA_EQUIPOS_REQ_X_FAMILIA, MA_DEQUIPOS_REQ_X_FAMILIA | — (≈20.000 filas) |
| MA_ENC_CONFIG_AVANCE, MA_DET_CONFIG_AVANCE, MA_SUBDET_CONFIG_AVANCE | — |
| MA_ENC_SEMANA_OT, MA_DET_SEMANA_OT, MA_TIPO_ORDEN_TRABAJO, MA_MST_FAMILIA | — |
| MA_OT_CONFIG_AVANCE | `ID_PERIODO = 30` (solo el período actual; la tabla tiene 760.000 filas) |
| PG_PERIODO | — (si el export trae columnas distintas a ID_EMPRESA, ID_PERIODO, DESCRIPCION, avisar) |

La empresa de CAC es la 30 (`SPRINT0_ID_EMPRESA`, ya es el valor por defecto).

## B. SAP

**Alcance:** las familias que se quieran validar (sugerido: las de un reporte completo de
disponibilidad, p. ej. "Equipos De Riegos - Gerencial", y una familia del Programa de Reparación),
**julio a septiembre de 2026**, incluyendo las órdenes abiertas a la fecha del export.

| # | Qué | De dónde | Columnas que importan |
|---|---|---|---|
| 1 | Maestro de equipos | **SE37 → `Z_PM_MF_WS_MAESTRO_EQUIPO`** (ya entregado por el ABAP), resultado a Excel. Alternativa: IH08 con el número de flota | EQUNR, EQKTX, **KEY_NUM / FLEET_NUM (la Ficha)**, ESTADO_EQUIPO |
| 2 | Órdenes | IW39 (abiertas, en tratamiento y concluidas; todas las clases) | Orden, Clase de orden, Clase de actividad PM, Equipo, Texto breve, Status del sistema, Status de usuario, **Fecha y hora de creación**, Inicio/Fin extremo, Inicio/Fin real (con hora si se puede), **Fecha de cierre técnico**, Aviso |
| 3 | Operaciones | IW37N, mismas órdenes | Orden, Operación, Texto breve, Puesto de trabajo, Trabajo + unidad, Trabajo real, Fechas de inicio/fin más tempranas, **Creado por** |
| 4 | Confirmaciones | IW47, mismas órdenes | Orden, Operación, Notificación, Trabajo real + unidad, Fecha de contabilización, **Fecha de entrada**, Notificación final, Anulado |
| 5 | Avisos | **SE37 → `Z_PM_MF_WS_AVISO_MANTENIMIENTO`** (ya entregado) o IW29 | QMNUM, QMART, EQUNR, AUFNR, fecha/hora del aviso, **MSAUS (parada)**, inicio/fin de la avería con hora |
| 6 | (Muy valioso) Historial de estados | SE16N tabla JCDS, objetos "OR" + órdenes del punto 2 | OBJNR, STAT, CHGNR, UDATE, UTIME, CHIND |

Lo que pasa si falta algo (el importador lo avisa y sigue):
- Sin **hora de creación** ni JCDS: se asume 12:00 del día de creación.
- Sin JCDS: el cierre técnico se toma de la fecha de cierre técnico y la hora de fin real si es el
  mismo día; si no, 12:00.
- Sin **IW47**: la semana de una actividad terminada sale de su fecha de fin real.
- Sin **avisos**: las correctivas cuentan desde la creación de la orden, sin mirar la parada.
- Equipos **sin Ficha** en SAP: quedan en "Equipos sin clasificar" y no cruzan con Oracle.

## C. Comparación contra Oracle

Pendiente de la salida de `docs/oracle/explorar_maquinarian_2.sql` (secciones 16-17: el PL/SQL que
llena `MA_DISPONIBILIDAD_EQUIPOS` y `MA_RES_CONFIG_AVANCE`). Con eso se define qué exportar de los
resultados de Oracle para julio-septiembre y se arma la comparación día por día. Criterio de
aceptación todavía por definir (propuesta: diferencia de 5 puntos o menos en al menos el 80 % de
los días por asignación).

---

## Referencia: definición original de los exports (30/09/2026)

Objetivo (sección 6.3 de la especificación): comparar la disponibilidad que calcula el portal a
partir de las órdenes de SAP contra lo que taller sabe que pasó, en un período ya cerrado, antes
de depender del RFC.

## Alcance del export

- **Familias:** 2 o 3 representativas (sugerido: Tractores y Vagones; si se puede, una chica como
  Cosechadoras).
- **Período:** **julio a septiembre de 2026** (SAP PM salió en vivo el 01/06/2026 y se estabilizó
  en julio). Si el Sistema de Órdenes de Trabajo de Oracle sigue registrando en paralelo, ese mismo
  período sirve para comparar contra sus reportes. Incluir órdenes abiertas a la fecha del export: una orden que empezó en el período y
  sigue abierta también cuenta.
- **Formato:** Excel (.xlsx) tal como lo baja SAP, con la fila de encabezados. No hace falta
  renombrar columnas ni quitar las que sobren.

## 1. Órdenes — IW39 (Visualizar órdenes PM)

Filtro: status **abiertas, en tratamiento y concluidas** (las tres casillas); período de fechas
del alcance; equipos de las familias elegidas (o el puesto de trabajo / emplazamiento que las
agrupe). Todas las clases de orden (ZC01, ZC03, ...).

Columnas del layout:

| Columna en SAP | Campo | Para qué |
|---|---|---|
| Orden | AUFNR | llave |
| Clase de orden | AUART | ZC03 = avance; todas = disponibilidad |
| Equipo | EQUNR | cruce con el maestro de equipos |
| Texto breve | KTEXT | detalle de órdenes del día |
| Puesto de trabajo responsable | VAPLZ | contexto |
| Status del sistema | STTXT | estado de la orden |
| Status de usuario | USTXT | estado de la orden ("En Proceso" suele ser de usuario) |
| Fecha y hora de creación | ERDAT / ERFZEIT | **inicio de la indisponibilidad** (la orden creada ya deja el equipo fuera) |
| Clase de actividad PM | ILART | separar planificada / preventiva / correctiva |
| Aviso | QMNUM | enlace con el aviso |
| Fecha de inicio extrema / básica | GSTRP | plan |
| Fecha de fin extrema / básica | GLTRP | plan |
| Fecha de inicio real | GSTRI | **inicio de la indisponibilidad** |
| Fecha de fin real / Fecha de cierre técnico | GLTRI / IDAT2 | **fin de la indisponibilidad** |
| Fecha de modificación | AEDAT | contexto |

## 2. Equipos — IH08 (Visualizar equipos)

Filtro: los equipos de las familias elegidas.

| Columna en SAP | Campo | Para qué |
|---|---|---|
| Equipo | EQUNR | llave |
| Denominación | EQKTX | descripción |
| **Código legado** (el campo donde se cargó el código viejo de Oracle) | ? | **cruce con la familia en Oracle** |

El código legado es el dato clave del cruce y la especificación no sabe en qué campo vive. Suele
estar en "Nº identificación técnica" (TIDNR) o "Nº de inventario" (INVNR), o en una
característica de clasificación. El ABAP lo sabe: es el mismo pendiente del Anexo A.5.

## 3. Horas por operación — IW37N (Resumen de operaciones)

Mismas órdenes del punto 1.

| Columna en SAP | Campo | Para qué |
|---|---|---|
| Orden | AUFNR | llave |
| Operación | VORNR | llave |
| Trabajo | ARBEI | horas planificadas |
| Trabajo real | ISMNW | horas confirmadas |
| Unidad de trabajo | ARBEH | verificar que todo esté en horas |

## 3b. Avisos — IW29 (Visualizar avisos)

Avisos de los mismos equipos y período: Aviso (QMNUM), Clase (QMART), Texto (QMTXT), Equipo,
Orden, Fecha/hora del aviso, **Parada (MSAUS)**, **Inicio y fin de la avería (AUSVN/AUZTV,
AUSBS/AUZTB)**, Duración de la parada, Status.

## 3c. Confirmaciones — IW47 (Visualizar confirmaciones)

Mismas órdenes: Orden, Operación, Trabajo real, Fecha de contabilización, Fecha de captura,
Confirmación final, Anulada. Sirve para saber en qué semana se terminó cada actividad.

## 4. (Opcional, muy valioso) Historial de estados — tabla JCDS

No hay transacción estándar cómoda; lo baja el ABAP con SE16N para las órdenes del punto 1:
OBJNR (= "OR" + número de orden), STAT, CHGNR, UDATE, UTIME, CHIND. Con esto se reconstruye el
estado exacto de cada equipo cualquier día (Anexo A.1) y el Sprint 0 se vuelve una simple
verificación.

## 5. Desde Oracle (lo prepara el DBA)

Para comparar: el reporte de disponibilidad de Oracle Forms (o su tabla de snapshot) de las
mismas familias y el mismo período, con la disponibilidad por día o por semana.

## Qué hace el portal con esto

Los archivos se cargan al staging con un importador de Excel (una tercera fuente de datos además
de "Local" y "RFC"), se corre el mismo cálculo que en producción y se compara día por día contra
lo que dice Oracle / taller. El criterio de aceptación (p. ej. diferencia menor a X puntos en al
menos el 80 % de los días) sigue pendiente de definir.
