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
--
-- Las tablas de origen están en el esquema MAQUINARIAN. Los nombres entre <...> marcados como
-- "candidata" salen de los bloques de las pantallas (F5, 02/10/2026) y hay que confirmarlos con
-- la salida de explorar_maquinarian.sql: un bloque de Forms no siempre es una tabla.
-- =====================================================================================

-- Equipos (Objeto Técnico / Ficha) con su familia: cruce SAP <-> Oracle por código legado.
CREATE OR REPLACE VIEW VW_EQUIPOS_FAMILIA_PGPM AS
SELECT <ficha>           AS CODIGO_LEGADO,   -- "F01168": mismo código que SAP trae como legado
       <equipo>          AS EQUIPO_CODIGO,   -- "A29017-002": código de equipo que muestra Oracle
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
FROM   <tabla_tipo_reporte>;            -- candidata: MAQUINARIAN.MA_CONFIG_DISPONIBILIDAD

-- Paso 2: semanas del reporte (generadas del rango de fechas).
CREATE OR REPLACE VIEW VW_DISP_SEMANA_PGPM AS
SELECT <id_reporte> AS REPORTE_ID, <no_semana> AS NO_SEMANA, <mes> AS MES,
       <fecha_inicio> AS FECHA_INICIO, <fecha_fin> AS FECHA_FIN
FROM   <tabla_semana_reporte>;          -- candidata: MAQUINARIAN.MA_CONFIG_SEMANA_DISP

-- Paso 3: horas del día en que corre el proceso (0 a 23).
CREATE OR REPLACE VIEW VW_DISP_HORA_PGPM AS
SELECT <id_reporte> AS REPORTE_ID,
       <hora 0-23>  AS HORA,             -- convertir si se guarda como '12:00 AM'
       CASE WHEN UPPER(<estado>) = 'ACTIVO' THEN 'S' ELSE 'N' END AS ACTIVO
FROM   <tabla_hora_reporte>;            -- candidata: MAQUINARIAN.MA_CONFIG_HORA_DISP

-- Paso 4: asignaciones (filas del reporte). Un "Grupo" junta equipos de distinta naturaleza
-- que entre todos resuelven una operación (p. ej. "Aguacatico Goteo"). ORDEN debe dejar cada
-- fila de grupo seguida de las filas de sus equipos, como en la pantalla: el portal toma como
-- miembros del grupo las filas que siguen hasta el próximo grupo, y en "Todas" cuenta solo el grupo.
CREATE OR REPLACE VIEW VW_DISP_ASIGNACION_PGPM AS
SELECT <id_reporte> AS REPORTE_ID, <id_asignacion> AS ASIGNACION_ID, <orden> AS ORDEN,
       <asignacion> AS ASIGNACION, <cant_asig> AS CANT_ASIGNADA, <cant_req> AS CANT_REQUERIDA,
       <disp_comp>  AS DISP_COMP_PCT,
       CASE WHEN UPPER(<negrita>) IN ('SI', 'S') THEN 'S' ELSE 'N' END AS NEGRITA,
       CASE WHEN UPPER(<grupo>)   IN ('SI', 'S') THEN 'S' ELSE 'N' END AS GRUPO
FROM   <tabla_asignacion>;              -- candidata: MAQUINARIAN.MA_EQUIPOS_REQ_X_FAMILIA

-- Equipos de cada asignación ("Detalle de Equipos Asignados"). La fila de grupo lleva todos
-- los equipos del grupo; cada fila miembro, el suyo.
CREATE OR REPLACE VIEW VW_DISP_ASIG_EQUIPO_PGPM AS
SELECT <id_reporte> AS REPORTE_ID, <id_asignacion> AS ASIGNACION_ID, <ficha> AS CODIGO_LEGADO
FROM   <tabla_asignacion_equipo>;       -- candidata: MAQUINARIAN.MA_DEQUIPOS_REQ_X_FAMILIA

-- =============================================================== avance

CREATE OR REPLACE VIEW VW_AVANCE_PERIODO_PGPM AS
SELECT <id_periodo> AS PERIODO_ID, <descripcion> AS DESCRIPCION   -- 30 / '2025 - 2026'
FROM   <tabla_periodo>;

-- Paso 1 de "Configuración Reporte Avance".
CREATE OR REPLACE VIEW VW_AVANCE_REPORTE_PGPM AS
SELECT <id_reporte> AS REPORTE_ID, <id_periodo> AS PERIODO_ID, <nombre_reporte> AS NOMBRE,
       <tipo_ot> AS TIPO_OT, <fecha_ref> AS FECHA_REF,
       <fecha_inicio_calendario> AS FECHA_INICIO, <fecha_fin_calendario> AS FECHA_FIN
FROM   <tabla_config_avance>;           -- candidata: MAQUINARIAN.MA_ENC_CONFIG_AVANCE

-- Paso 3: familias del reporte.
CREATE OR REPLACE VIEW VW_AVANCE_FAMILIA_PGPM AS
SELECT <id_reporte> AS REPORTE_ID, <orden> AS ORDEN, <familia> AS FAMILIA_CODIGO,
       <familia_nombre> AS FAMILIA_NOMBRE, <fecha_est_inicio> AS FECHA_EST_INICIO,
       <fecha_est_fin> AS FECHA_EST_FIN, <cant_equipo> AS CANT_EQUIPO
FROM   <tabla_familia_definida>;        -- candidata: MAQUINARIAN.MA_DET_CONFIG_AVANCE

-- "Órdenes de Trabajo Asignada a Familia": qué equipos forman el programa de cada familia.
-- El portal cruza con SAP por CODIGO_LEGADO (el número de OT es el de Oracle, no el de SAP).
CREATE OR REPLACE VIEW VW_AVANCE_OT_PGPM AS
SELECT <id_reporte> AS REPORTE_ID, <familia> AS FAMILIA_CODIGO, <no_ot> AS NO_OT,
       <ficha> AS CODIGO_LEGADO, <cant_actividad> AS CANT_ACTIVIDAD, <cant_dias> AS CANT_DIAS
FROM   <tabla_ot_asignada>;             -- candidata: MAQUINARIAN.MA_OT_CONFIG_AVANCE

-- "Semana Definida Por Familia": actividades y días planificados de cada semana (no
-- acumulados). FECHA_INICIO/FIN salen del calendario del reporte.
CREATE OR REPLACE VIEW VW_AVANCE_SEMANA_PGPM AS
SELECT <id_reporte> AS REPORTE_ID, <familia> AS FAMILIA_CODIGO, <no_semana> AS NO_SEMANA,
       <mes> AS MES, <fecha_inicio_semana> AS FECHA_INICIO, <fecha_fin_semana> AS FECHA_FIN,
       <actividad> AS ACTIVIDAD, <dias> AS DIAS
FROM   <tabla_semana_familia>;          -- candidata: MAQUINARIAN.MA_SUBDET_CONFIG_AVANCE
