# Sprint 0 — Exports de SAP para validar el cálculo con datos reales

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
