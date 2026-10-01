-- =====================================================================================
-- PRODUCCIÓN — Vistas de estructura del Portal Gerencial (sección 4.1).
-- Lo corre el DBA en el esquema del portal (placeholder: PGPM), una vez y cada vez que
-- cambie alguna tabla de origen. Es idempotente (CREATE OR REPLACE).
--
-- Reemplazar cada <...> por la tabla/columna real del módulo "Sistema de Órdenes de
-- Trabajo" de Oracle Forms. NO cambiar los nombres de vista ni los alias de columna: son
-- el contrato con el código del portal (ver db/oracle/init/03_vistas_pgpm.sql, la versión
-- local que apunta al esquema simulado SISTEMA_OT).
--
-- El dueño de las vistas necesita GRANT SELECT directo (no por rol) sobre cada tabla.
-- =====================================================================================

-- Tipos de reporte de disponibilidad y umbrales de semáforo (Paso 1 de "Configuración
-- Reporte Disponibilidad"). Disponibilidad < UMBRAL_ROJO = rojo; < UMBRAL_NARANJA = naranja.
CREATE OR REPLACE VIEW VW_UMBRALES_SEMAFORO_PGPM AS
SELECT <tipo_reporte>    AS TIPO_REPORTE,     -- VARCHAR2: código del tipo de reporte
       <descripcion>     AS DESCRIPCION,      -- VARCHAR2: texto que ve el usuario en el selector
       <umbral_rojo>     AS UMBRAL_ROJO,      -- NUMBER: % 0-100
       <umbral_naranja>  AS UMBRAL_NARANJA    -- NUMBER: % 0-100
FROM   <tabla_tipo_reporte>;

-- Familias y su ventana de reparación por programa/año (Paso 3 de "Configuración Reporte
-- Avance OT").
CREATE OR REPLACE VIEW VW_ESTRUCTURA_FAMILIAS_PGPM AS
SELECT <anio_programa>     AS ANIO_PROGRAMA,     -- VARCHAR2
       <familia_codigo>    AS FAMILIA_CODIGO,    -- VARCHAR2
       <familia_nombre>    AS FAMILIA_NOMBRE,    -- VARCHAR2
       <fecha_est_inicio>  AS FECHA_EST_INICIO,  -- DATE
       <fecha_est_fin>     AS FECHA_EST_FIN,     -- DATE
       <cant_equipo>       AS CANT_EQUIPO        -- NUMBER
FROM   <tabla_familia_definida>;

-- Curva de avance planeado por familia y semana (Paso 3, "Semana Definida Por Familia").
-- ACTIVIDAD_PCT es el % de la actividad total programado para ESA semana (no acumulado):
-- el portal lo acumula. Si en Oracle ya viene acumulado, avisar para ajustar el cálculo.
CREATE OR REPLACE VIEW VW_CURVA_PLANEADA_PGPM AS
SELECT <anio_programa>        AS ANIO_PROGRAMA,        -- VARCHAR2
       <familia_codigo>       AS FAMILIA_CODIGO,       -- VARCHAR2
       <mes>                  AS MES,                  -- NUMBER
       <no_semana>            AS NO_SEMANA,            -- NUMBER: 1, 2, 3... dentro del programa
       <fecha_inicio_semana>  AS FECHA_INICIO_SEMANA,  -- DATE
       <fecha_fin_semana>     AS FECHA_FIN_SEMANA,     -- DATE
       <actividad_pct>        AS ACTIVIDAD_PCT,        -- NUMBER: % de esa semana
       <dias>                 AS DIAS                  -- NUMBER: días laborables
FROM   <tabla_semana_familia>;

-- Equipos requeridos por familia y asignación / proceso productivo (Paso 4 de
-- "Configuración Reporte Disponibilidad").
CREATE OR REPLACE VIEW VW_EQUIPOS_REQUERIDOS_PGPM AS
SELECT <tipo_reporte>    AS TIPO_REPORTE,    -- VARCHAR2
       <familia_codigo>  AS FAMILIA_CODIGO,  -- VARCHAR2
       <asignacion>      AS ASIGNACION,      -- VARCHAR2: proceso productivo (TIRO, CORTE...)
       <cant_asignada>   AS CANT_ASIGNADA,   -- NUMBER
       <cant_requerida>  AS CANT_REQUERIDA   -- NUMBER
FROM   <tabla_equipos_requeridos>;

-- Equipos (Objeto Técnico / Ficha) con su familia. No estaba en la especificación, pero es
-- el cruce SAP <-> familia por código legado y el detalle de equipos por familia.
CREATE OR REPLACE VIEW VW_EQUIPOS_FAMILIA_PGPM AS
SELECT <codigo_legado>   AS CODIGO_LEGADO,   -- VARCHAR2: mismo código que SAP trae como legado
       <descripcion>     AS DESCRIPCION,     -- VARCHAR2
       <familia_codigo>  AS FAMILIA_CODIGO,  -- VARCHAR2
       <activo>          AS ACTIVO           -- 'S' / 'N' (traducir si en Oracle es otro valor)
FROM   <tabla_equipos>;
