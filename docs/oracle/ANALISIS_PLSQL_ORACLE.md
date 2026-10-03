# Cómo calcula Oracle los dos reportes (PL/SQL leído el 03/10/2026)

Fuente: `explorar_maquinarian_2.sql`, secciones 15 a 17 (`docs/oracle/15.xlsx` a `17.xlsx`). Esquema
MAQUINARIAN, empresa **30**. No hay jobs de Oracle a la vista (sección 18 vacía): el proceso se dispara
desde otro lado (Forms o un job de otro usuario, p. ej. `PARGER`).

## Disponibilidad — `PR_DISPONIBILIDAD_EQUIPOS` + `PR_LLENAR_TEMP_DISPONIBILIDAD`

Es una **foto en el momento** a cada hora configurada (`MA_CONFIG_HORA_DISP`, texto `' 00:00'`...
`' 23:00'`), solo de los reportes `ACTIVO`. No reconstruye el pasado: si el proceso no corrió a una
hora, esa hora no existe.

Por cada asignación (`MA_EQUIPOS_REQ_X_FAMILIA` con `CANT_REQ <> 0`):

| Dato | Regla de Oracle | Portal |
|---|---|---|
| Equipos (CANT_EQ) | equipos de la asignación con `ESTADO = 'ACTIVO'`, sin las fichas terminadas en "000" | Igual (vistas ajustadas el 03/10). Además excluye los inactivos en SAP |
| Fuera de servicio | equipo con una **OT de Oracle `ID_TIPO_OT = 1` en estado 1, 2 o 4** abierta en ese momento | Orden SAP de las clases `DISPONIBILIDAD.CLASES_ORDEN` (hoy ZC01 correctiva **y ZC02 preventiva**) desde su creación hasta el cierre técnico; correctiva con aviso según la parada |
| **Disponibilidad %** (`PORCIENTO_HORA`) | `ROUND(disponibles / EQUIPOS * 100)` | `disponibles / REQUERIDOS`, topado en 100 (decisión del 30/09). Nuevo parámetro `DISPONIBILIDAD.BASE_PORCENTAJE` = `EQUIPOS` para calcular como Oracle |
| Potencial % (`PORCIENTO_FIA`) | `ROUND(disponibles / requeridos * 100)` | Igual (sin tope) |
| Disponibles = 0 | se guarda 0.0001 | 0 |
| Día | promedio de las horas medidas | Igual |
| Semana | promedio de los días con datos | Igual |
| Todas (columnas actuales) | `SUM(disponibles) / SUM(equipos)` de **todas** las filas | Total disponible / total requerido sin que el sobrante de una tape a otra (decisión del 30/09); con base EQUIPOS, `SUM(disp)/SUM(equipos)` como Oracle |
| Todas (semanas) | promedio simple de los % diarios de todas las filas | Igual que las columnas actuales (total / total) |
| Grupos | no tiene lógica de grupo: las filas de un grupo y las de sus equipos suman las dos en Todas | La fila incluida en un grupo no se cuenta dos veces |

## Avance — `PR_ACTUALIZAR_CONFIG_AVANCE` + `PR_RES_CONFIG_SEMANA` + `VW_RES_CONFIG_AVANCE`

Todo se mide en **cantidad de actividades**, a partir de las OT **de Oracle** asignadas
(`MA_OT_CONFIG_AVANCE`, una fila por actividad con su semana planificada) y del estado de cada
actividad en `MA_SUBDETALLE_ORDEN_TRABAJO` (última secuencia).

| Dato | Regla de Oracle | Portal |
|---|---|---|
| Actividad terminada | última secuencia en estado **3 (Terminada) o 5 (No trabajada)** | Operación SAP con horas reales |
| Semana en que cuenta | fecha inicio y fin reales de la actividad **dentro** de la semana | Fecha en que se confirmaron las horas |
| Real Programado (semana) | actividades **planificadas para esa semana** (`SELECCIONADO = 'S'`) terminadas dentro de esa semana | Actividades del plan original (creadas por el usuario de planificación) terminadas esa semana |
| Real No Programado (semana) | actividades **planificadas para otra semana** terminadas dentro de esta | Actividades agregadas en la reparación (creadas por otro usuario) |
| % semanal guardado (`REAL_*_PORC`) | cantidad / actividades planificadas **de esa semana** | — |
| % que se muestra (`PORC_AVANCE_*`) | ejecutado / total de actividades planificadas de la familia (`SUM(CANT_ACTIVIDAD)`) | Igual con `AVANCE.BASE_PLAN=ACTIVIDADES`; hoy está en `DIAS` (respuesta del 02/10) |
| Prog. a la fecha | actividades planificadas de las semanas anteriores a la actual | Igual (Prog. Cierre) |
| Nombre de la familia | `INITCAP(MA_MST_FAMILIA.DESCRIPCION)` | Igual (vista ajustada) |

## Qué implica para el Sprint 0

- **Disponibilidad:** para comparar número con número contra Oracle hay que correr el portal con
  `DISPONIBILIDAD.BASE_PORCENTAJE=EQUIPOS` y `DISPONIBILIDAD.CLASES_ORDEN` con solo la clase SAP
  que equivale a la OT tipo 1 de Oracle. Las diferencias que queden serán de datos (fechas de
  creación y cierre en SAP vs. Oracle), que es lo que se quiere medir.
- **Avance:** el de Oracle depende de que el taller siga cerrando actividades en las OT **de
  Oracle**. Si las reparaciones de 2026 ya se registran en SAP, el avance de Oracle de 2026 está
  congelado y no sirve de referencia.
- Resultados de Oracle para comparar (cuando toque): `MA_DISPONIBILIDAD_EQUIPOS` (una fila por
  asignación y hora medida; julio a septiembre se puede resumir por día) y `MA_RES_CONFIG_AVANCE`.
