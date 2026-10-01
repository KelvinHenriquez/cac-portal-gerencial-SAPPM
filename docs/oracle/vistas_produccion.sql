-- =====================================================================================
-- PRODUCCIÓN — Vistas de estructura del Portal Gerencial SAP PM (Oracle 11g).
-- Lo corre el DBA en el esquema del portal, una vez y cada vez que cambie una tabla de
-- origen. Es idempotente (CREATE OR REPLACE).
--
-- Reemplazar cada <...> por la tabla/columna real del "Sistema de Órdenes de Trabajo" de
-- Oracle Forms: pantallas MA_CONFIGURACION_DISP (disponibilidad) y MA_CONFIG_AVANCE (avance).
-- NO cambiar los nombres de vista ni los alias de columna: son el contrato con el portal (la
-- versión local, sobre el esquema simulado SISTEMA_OT, está en db/oracle/init/03_vistas_pgpm.sql).
-- Banderas como 'S'/'N'. El dueño de las vistas necesita GRANT SELECT directo (no por rol).
-- =====================================================================================

-- Equipos (Objeto Técnico / Ficha) con su familia: cruce SAP <-> Oracle por código legado.
CREATE OR REPLACE VIEW VW_EQUIPOS_FAMILIA_PGPM AS
SELECT <ficha>           AS CODIGO_LEGADO,   -- mismo código que SAP trae como legado
       <descripcion>     AS DESCRIPCION,
       <familia>         AS FAMILIA_CODIGO,
       <activo 'S'/'N'>  AS ACTIVO
FROM   <tabla_equipos>;

-- =============================================================== disponibilidad

-- Paso 1: tipo de reporte. Rojo si < UMBRAL_ROJO (Comp. 1); naranja si < UMBRAL_NARANJA (Comp. 2).
CREATE OR REPLACE VIEW VW_DISP_REPORTE_PGPM AS
SELECT <id>          AS REPORTE_ID,
       <orden>       AS ORDEN,
       <descripcion> AS DESCRIPCION,     -- "Equipos De Riegos - Gerencial"...
       <comp1>       AS UMBRAL_ROJO,
       <comp2>       AS UMBRAL_NARANJA,
       CASE WHEN UPPER(<estado>) = 'ACTIVO' THEN 'S' ELSE 'N' END AS ACTIVO
FROM   <tabla_tipo_reporte>;

-- Paso 2: semanas del reporte (generadas del rango de fechas).
CREATE OR REPLACE VIEW VW_DISP_SEMANA_PGPM AS
SELECT <id_reporte> AS REPORTE_ID, <no_semana> AS NO_SEMANA, <mes> AS MES,
       <fecha_inicio> AS FECHA_INICIO, <fecha_fin> AS FECHA_FIN
FROM   <tabla_semana_reporte>;

-- Paso 3: horas del día en que corre el proceso (0 a 23).
CREATE OR REPLACE VIEW VW_DISP_HORA_PGPM AS
SELECT <id_reporte> AS REPORTE_ID,
       <hora 0-23>  AS HORA,             -- convertir si se guarda como '12:00 AM'
       CASE WHEN UPPER(<estado>) = 'ACTIVO' THEN 'S' ELSE 'N' END AS ACTIVO
FROM   <tabla_hora_reporte>;

-- Paso 4: asignaciones (filas del reporte).
CREATE OR REPLACE VIEW VW_DISP_ASIGNACION_PGPM AS
SELECT <id_reporte> AS REPORTE_ID, <id_asignacion> AS ASIGNACION_ID, <orden> AS ORDEN,
       <asignacion> AS ASIGNACION, <cant_asig> AS CANT_ASIGNADA, <cant_req> AS CANT_REQUERIDA,
       <disp_comp>  AS DISP_COMP_PCT,
       CASE WHEN UPPER(<negrita>) IN ('SI', 'S') THEN 'S' ELSE 'N' END AS NEGRITA,
       CASE WHEN UPPER(<grupo>)   IN ('SI', 'S') THEN 'S' ELSE 'N' END AS GRUPO
FROM   <tabla_asignacion>;

-- Equipos de cada asignación (botón "..." de Cant. Asig.). Si la asignación apunta a un
-- GRUPO de equipos, expandirlo aquí a sus equipos individuales (UNION de equipos directos +
-- equipos del grupo).
CREATE OR REPLACE VIEW VW_DISP_ASIG_EQUIPO_PGPM AS
SELECT <id_reporte> AS REPORTE_ID, <id_asignacion> AS ASIGNACION_ID, <ficha> AS CODIGO_LEGADO
FROM   <tabla_asignacion_equipo>;

-- =============================================================== avance

CREATE OR REPLACE VIEW VW_AVANCE_PERIODO_PGPM AS
SELECT <id_periodo> AS PERIODO_ID, <descripcion> AS DESCRIPCION   -- 30 / '2025 - 2026'
FROM   <tabla_periodo>;

-- Paso 1 de "Configuración Reporte Avance".
CREATE OR REPLACE VIEW VW_AVANCE_REPORTE_PGPM AS
SELECT <id_reporte> AS REPORTE_ID, <id_periodo> AS PERIODO_ID, <nombre_reporte> AS NOMBRE,
       <tipo_ot> AS TIPO_OT, <fecha_ref> AS FECHA_REF,
       <fecha_inicio_calendario> AS FECHA_INICIO, <fecha_fin_calendario> AS FECHA_FIN
FROM   <tabla_config_avance>;

-- Paso 3: familias del reporte.
CREATE OR REPLACE VIEW VW_AVANCE_FAMILIA_PGPM AS
SELECT <id_reporte> AS REPORTE_ID, <orden> AS ORDEN, <familia> AS FAMILIA_CODIGO,
       <familia_nombre> AS FAMILIA_NOMBRE, <fecha_est_inicio> AS FECHA_EST_INICIO,
       <fecha_est_fin> AS FECHA_EST_FIN, <cant_equipo> AS CANT_EQUIPO
FROM   <tabla_familia_definida>;

-- "Semana Definida Por Familia": actividades y días planificados de cada semana (no
-- acumulados). FECHA_INICIO/FIN salen del calendario del reporte.
CREATE OR REPLACE VIEW VW_AVANCE_SEMANA_PGPM AS
SELECT <id_reporte> AS REPORTE_ID, <familia> AS FAMILIA_CODIGO, <no_semana> AS NO_SEMANA,
       <mes> AS MES, <fecha_inicio_semana> AS FECHA_INICIO, <fecha_fin_semana> AS FECHA_FIN,
       <actividad> AS ACTIVIDAD, <dias> AS DIAS
FROM   <tabla_semana_familia>;
