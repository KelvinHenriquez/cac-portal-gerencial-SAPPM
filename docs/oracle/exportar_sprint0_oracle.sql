-- =====================================================================================
-- Sprint 0 — Lo que se necesita de Oracle además del export de configuración. SOLO SELECT.
-- SQL Developer, F5, y exportar cada resultado a Excel con el nombre indicado, en
-- C:\cac-portal-gerencial-SAPPM\sprint0\oracle_resultados\
-- =====================================================================================

PROMPT ===== 1. Tabla de períodos (PARGER.PG_PERIODO): columnas y filas -> periodos.xlsx =====
SELECT COLUMN_ID, COLUMN_NAME, DATA_TYPE, DATA_LENGTH
FROM   ALL_TAB_COLUMNS
WHERE  OWNER = 'PARGER' AND TABLE_NAME = 'PG_PERIODO'
ORDER BY COLUMN_ID;
SELECT * FROM PARGER.PG_PERIODO WHERE ID_EMPRESA = 30;

PROMPT ===== 2. Disponibilidad calculada por Oracle, julio a septiembre, por día -> oracle_disponibilidad.xlsx =====
-- Una fila por reporte, asignación y día (≈ 15.000 filas). PORCIENTO_HORA = disponibles / equipos,
-- PORCIENTO_FIA = disponibles / requeridos (ver ANALISIS_PLSQL_ORACLE.md).
SELECT ID_AREA_SERVICIO, ID_TIPO_REPORTE, CORRELATIVO, TRUNC(DIA) AS DIA,
       COUNT(*)                         AS HORAS_MEDIDAS,
       MAX(CANT_EQ)                     AS CANT_EQ,
       MAX(CANT_REQ)                    AS CANT_REQ,
       ROUND(AVG(CANT_DISP), 2)         AS DISP_PROMEDIO,
       ROUND(AVG(PORCIENTO_HORA), 2)    AS DISP_PCT,
       ROUND(AVG(PORCIENTO_FIA), 2)     AS POTENCIAL_PCT
FROM   MAQUINARIAN.MA_DISPONIBILIDAD_EQUIPOS
WHERE  ID_EMPRESA = 30
AND    ID_TIPO_REPORTE BETWEEN 51 AND 57
AND    DIA >= DATE '2026-07-01' AND DIA < DATE '2026-10-01'
GROUP BY ID_AREA_SERVICIO, ID_TIPO_REPORTE, CORRELATIVO, TRUNC(DIA)
ORDER BY ID_TIPO_REPORTE, CORRELATIVO, DIA;

PROMPT ===== 3. Órdenes de Oracle que dejaron equipos fuera, julio a septiembre -> oracle_ot_disponibilidad.xlsx =====
-- Para explicar diferencias equipo por equipo (qué OT de Oracle vs. qué orden de SAP).
SELECT D.ID_TIPO_REPORTE, D.CORRELATIVO, TRUNC(D.DIA) AS DIA, D.NO_ORDEN_TRABAJO, D.ID_TIPO_OT,
       COUNT(*) AS HORAS
FROM   MAQUINARIAN.MA_DISPONIBILIDAD_DEQUIPOS D
WHERE  D.ID_EMPRESA = 30
AND    D.ID_TIPO_REPORTE BETWEEN 51 AND 57
AND    D.DIA >= DATE '2026-07-01' AND D.DIA < DATE '2026-10-01'
GROUP BY D.ID_TIPO_REPORTE, D.CORRELATIVO, TRUNC(D.DIA), D.NO_ORDEN_TRABAJO, D.ID_TIPO_OT
ORDER BY D.ID_TIPO_REPORTE, D.CORRELATIVO, DIA;

PROMPT ===== 4. Tipos y estados de OT de Oracle -> oracle_tipos_ot.xlsx =====
SELECT ID_TIPO_OT, DESCRIPCION, ESTADO FROM MAQUINARIAN.MA_TIPO_ORDEN_TRABAJO WHERE ID_EMPRESA = 30 ORDER BY ID_TIPO_OT;
SELECT * FROM MAQUINARIAN.MA_ESTADO_ORDEN_TRABAJO;

-- El avance que calculó Oracle no hace falta exportarlo aparte: viene en las columnas REAL_* de
-- MA_SUBDET_CONFIG_AVANCE, que ya van en el export de configuración.
PROMPT ===== Fin =====
