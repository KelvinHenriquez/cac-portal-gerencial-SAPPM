-- =====================================================================================
-- PRODUCCIÓN, PASO 2 de 2 — Lo corre el USUARIO TÉCNICO DEL PORTAL (el creado en el paso 1),
-- no un DBA: las tablas y vistas tienen que quedar en su esquema.
-- SQL Developer: conexión con ese usuario, abrir este archivo y ejecutar como script (F5).
-- Este archivo tiene que estar en la misma carpeta que pgpm_tablas.sql y vistas_produccion.sql.
--
-- Qué hace (se puede volver a correr sin daño):
--   1. pgpm_tablas.sql       tablas del portal (*_PGPM): parámetros, datos traídos de SAP,
--                            resultados de los cálculos, bitácora. Solo crea lo que falta.
--   2. vistas_produccion.sql 11 vistas VW_*_PGPM que leen la configuración de Oracle Forms
--                            (MAQUINARIAN), empresa CAC = 30. CREATE OR REPLACE.
--   3. Verificación.
--
-- Con este script corrido, en el .env.prod del servidor se puede poner APLICAR_MIGRACIONES=false
-- (el portal no necesita crear nada al arrancar). Cuando una versión nueva del portal traiga
-- cambios de tablas, se entrega un pgpm_tablas.sql nuevo y se vuelve a correr este archivo.
-- =====================================================================================
SET DEFINE ON
SET VERIFY OFF
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT FAILURE

PROMPT ===== 1. Tablas del portal =====
@@pgpm_tablas.sql

PROMPT ===== 2. Vistas sobre Oracle Forms (empresa 30) =====
DEFINE ID_EMPRESA = 30
@@vistas_produccion.sql

PROMPT ===== 3. Verificación =====
PROMPT Objetos del portal (deben salir 0 inválidos):
SELECT OBJECT_TYPE, STATUS, COUNT(*) AS CANTIDAD
FROM   USER_OBJECTS
WHERE  OBJECT_NAME LIKE '%PGPM'
GROUP BY OBJECT_TYPE, STATUS
ORDER BY OBJECT_TYPE, STATUS;

PROMPT Filas que ve cada vista (ninguna debería dar error; las de configuración, más de 0):
SELECT 'VW_EQUIPOS_FAMILIA_PGPM'   AS VISTA, COUNT(*) AS FILAS FROM VW_EQUIPOS_FAMILIA_PGPM   UNION ALL
SELECT 'VW_DISP_REPORTE_PGPM',               COUNT(*)          FROM VW_DISP_REPORTE_PGPM      UNION ALL
SELECT 'VW_DISP_ASIGNACION_PGPM',            COUNT(*)          FROM VW_DISP_ASIGNACION_PGPM   UNION ALL
SELECT 'VW_DISP_ASIG_EQUIPO_PGPM',           COUNT(*)          FROM VW_DISP_ASIG_EQUIPO_PGPM  UNION ALL
SELECT 'VW_DISP_SEMANA_PGPM',                COUNT(*)          FROM VW_DISP_SEMANA_PGPM       UNION ALL
SELECT 'VW_DISP_HORA_PGPM',                  COUNT(*)          FROM VW_DISP_HORA_PGPM         UNION ALL
SELECT 'VW_AVANCE_REPORTE_PGPM',             COUNT(*)          FROM VW_AVANCE_REPORTE_PGPM    UNION ALL
SELECT 'VW_AVANCE_FAMILIA_PGPM',             COUNT(*)          FROM VW_AVANCE_FAMILIA_PGPM    UNION ALL
SELECT 'VW_AVANCE_PERIODO_PGPM',             COUNT(*)          FROM VW_AVANCE_PERIODO_PGPM    UNION ALL
SELECT 'VW_AVANCE_SEMANA_PGPM',              COUNT(*)          FROM VW_AVANCE_SEMANA_PGPM     UNION ALL
SELECT 'VW_AVANCE_OT_PGPM',                  COUNT(*)          FROM VW_AVANCE_OT_PGPM;

PROMPT Listo. En el servidor: APLICAR_MIGRACIONES=false en .env.prod y levantar el portal.
