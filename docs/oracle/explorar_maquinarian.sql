-- =====================================================================================
-- Exploración del esquema MAQUINARIAN (Sistema de Órdenes de Trabajo) — SOLO LECTURA.
--
-- Qué hace: consulta el DICCIONARIO de datos de Oracle (nombres de tablas, columnas, llaves y
-- vistas). No lee filas de ninguna tabla de negocio, no crea, no modifica ni borra nada.
--
-- Cómo correrlo (SQL Developer, con tu usuario, que ya tiene acceso a MAQUINARIAN):
--   1. Abrir este archivo y ejecutarlo como script (F5).
--   2. Guardar la salida ("Script Output") como texto en:
--        C:\cac-portal-gerencial-SAPPM\docs\oracle\exploracion_maquinarian.txt
--   3. Avisar: con esa salida se llenan las vistas reales (vistas_produccion.sql).
--
-- Los nombres vienen de los bloques de las pantallas MA_CONFIGURACION_DISP y MA_CONFIG_AVANCE
-- (capturas del 02/10/2026); algunos pueden ser bloques de Forms y no tablas: por eso también
-- se buscan tablas parecidas por patrón.
-- =====================================================================================
SET PAGESIZE 5000
SET LINESIZE 220
SET TRIMSPOOL ON
COLUMN TABLE_NAME      FORMAT A34
COLUMN COLUMN_NAME     FORMAT A32
COLUMN TIPO            FORMAT A22
COLUMN NULO            FORMAT A4
COLUMN CONSTRAINT_NAME FORMAT A32
COLUMN R_TABLA         FORMAT A34
COLUMN COLUMNAS        FORMAT A70
COLUMN COMENTARIO      FORMAT A70

PROMPT ===== 1. Tablas candidatas (y cuántas filas dicen las estadísticas) =====
SELECT t.TABLE_NAME, t.NUM_ROWS, t.LAST_ANALYZED
FROM   ALL_TABLES t
WHERE  t.OWNER = 'MAQUINARIAN'
AND   (   t.TABLE_NAME IN ('MA_CONFIG_DISPONIBILIDAD', 'MA_CONFIG_SEMANA_DISP', 'MA_CONFIG_HORA_DISP',
                           'MA_EQUIPOS_REQ_X_FAMILIA', 'MA_DEQUIPOS_REQ_X_FAMILIA', 'CF_AREA',
                           'MA_ENC_CONFIG_AVANCE', 'MA_DET_CONFIG_AVANCE', 'MA_SUBDET_CONFIG_AVANCE',
                           'MA_OT_CONFIG_AVANCE', 'MA_MST_ORDEN_TRABAJO_TMP')
       OR t.TABLE_NAME LIKE 'MA\_%CONFIG%'  ESCAPE '\'
       OR t.TABLE_NAME LIKE 'MA\_%DISP%'    ESCAPE '\'
       OR t.TABLE_NAME LIKE 'MA\_%AVANCE%'  ESCAPE '\'
       OR t.TABLE_NAME LIKE 'MA\_%EQUIPO%'  ESCAPE '\'
       OR t.TABLE_NAME LIKE 'MA\_%FAMILIA%' ESCAPE '\'
       OR t.TABLE_NAME LIKE 'MA\_MST\_%'    ESCAPE '\'
       OR t.TABLE_NAME LIKE 'MA\_%ORDEN%'   ESCAPE '\'
       OR t.TABLE_NAME LIKE 'MA\_%ACTIVIDAD%' ESCAPE '\'
       OR t.TABLE_NAME LIKE '%CALENDARIO%'
       OR t.TABLE_NAME LIKE '%PERIODO%'
       OR t.TABLE_NAME LIKE '%SEMANA%')
ORDER BY t.TABLE_NAME;

PROMPT ===== 2. Columnas de esas tablas =====
SELECT c.TABLE_NAME, c.COLUMN_ID, c.COLUMN_NAME,
       c.DATA_TYPE || CASE WHEN c.DATA_TYPE IN ('VARCHAR2', 'CHAR') THEN '(' || c.DATA_LENGTH || ')'
                           WHEN c.DATA_TYPE = 'NUMBER' AND c.DATA_PRECISION IS NOT NULL
                                THEN '(' || c.DATA_PRECISION || ',' || c.DATA_SCALE || ')' END AS TIPO,
       c.NULLABLE AS NULO
FROM   ALL_TAB_COLUMNS c
WHERE  c.OWNER = 'MAQUINARIAN'
AND   (   c.TABLE_NAME LIKE 'MA\_%CONFIG%'  ESCAPE '\'
       OR c.TABLE_NAME LIKE 'MA\_%DISP%'    ESCAPE '\'
       OR c.TABLE_NAME LIKE 'MA\_%AVANCE%'  ESCAPE '\'
       OR c.TABLE_NAME LIKE 'MA\_%EQUIPO%'  ESCAPE '\'
       OR c.TABLE_NAME LIKE 'MA\_%FAMILIA%' ESCAPE '\'
       OR c.TABLE_NAME LIKE 'MA\_MST\_%'    ESCAPE '\'
       OR c.TABLE_NAME LIKE 'MA\_%ORDEN%'   ESCAPE '\'
       OR c.TABLE_NAME LIKE 'MA\_%ACTIVIDAD%' ESCAPE '\'
       OR c.TABLE_NAME LIKE '%CALENDARIO%'
       OR c.TABLE_NAME LIKE '%PERIODO%'
       OR c.TABLE_NAME LIKE '%SEMANA%'
       OR c.TABLE_NAME = 'CF_AREA')
ORDER BY c.TABLE_NAME, c.COLUMN_ID;

PROMPT ===== 3. Llaves primarias y foráneas (cómo se enlazan) =====
SELECT k.TABLE_NAME, k.CONSTRAINT_TYPE AS T, k.CONSTRAINT_NAME,
       (SELECT r.TABLE_NAME FROM ALL_CONSTRAINTS r
        WHERE r.OWNER = k.R_OWNER AND r.CONSTRAINT_NAME = k.R_CONSTRAINT_NAME) AS R_TABLA,
       (SELECT LISTAGG(cc.COLUMN_NAME, ', ') WITHIN GROUP (ORDER BY cc.POSITION)
        FROM ALL_CONS_COLUMNS cc
        WHERE cc.OWNER = k.OWNER AND cc.CONSTRAINT_NAME = k.CONSTRAINT_NAME) AS COLUMNAS
FROM   ALL_CONSTRAINTS k
WHERE  k.OWNER = 'MAQUINARIAN'
AND    k.CONSTRAINT_TYPE IN ('P', 'R', 'U')
AND   (   k.TABLE_NAME LIKE 'MA\_%CONFIG%'  ESCAPE '\'
       OR k.TABLE_NAME LIKE 'MA\_%DISP%'    ESCAPE '\'
       OR k.TABLE_NAME LIKE 'MA\_%AVANCE%'  ESCAPE '\'
       OR k.TABLE_NAME LIKE 'MA\_%EQUIPO%'  ESCAPE '\'
       OR k.TABLE_NAME LIKE 'MA\_%FAMILIA%' ESCAPE '\'
       OR k.TABLE_NAME LIKE 'MA\_MST\_%'    ESCAPE '\'
       OR k.TABLE_NAME LIKE '%CALENDARIO%'
       OR k.TABLE_NAME LIKE '%PERIODO%'
       OR k.TABLE_NAME LIKE '%SEMANA%')
ORDER BY k.TABLE_NAME, k.CONSTRAINT_TYPE, k.CONSTRAINT_NAME;

PROMPT ===== 4. Comentarios de columnas (si los hay) =====
SELECT m.TABLE_NAME, m.COLUMN_NAME, m.COMMENTS AS COMENTARIO
FROM   ALL_COL_COMMENTS m
WHERE  m.OWNER = 'MAQUINARIAN'
AND    m.COMMENTS IS NOT NULL
AND   (   m.TABLE_NAME LIKE 'MA\_%CONFIG%' ESCAPE '\'
       OR m.TABLE_NAME LIKE 'MA\_%DISP%'   ESCAPE '\'
       OR m.TABLE_NAME LIKE 'MA\_%AVANCE%' ESCAPE '\'
       OR m.TABLE_NAME LIKE 'MA\_%EQUIPO%' ESCAPE '\')
ORDER BY m.TABLE_NAME, m.COLUMN_NAME;

PROMPT ===== 5. Vistas del esquema que ya resuelvan algo de esto =====
SELECT v.VIEW_NAME
FROM   ALL_VIEWS v
WHERE  v.OWNER = 'MAQUINARIAN'
AND   (v.VIEW_NAME LIKE '%DISP%' OR v.VIEW_NAME LIKE '%AVANCE%' OR v.VIEW_NAME LIKE '%EQUIPO%' OR v.VIEW_NAME LIKE '%FAMILIA%')
ORDER BY v.VIEW_NAME;

PROMPT ===== Fin. Guardar esta salida en docs\oracle\exploracion_maquinarian.txt =====
