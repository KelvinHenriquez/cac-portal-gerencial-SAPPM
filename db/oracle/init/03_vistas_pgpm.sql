-- =====================================================================================
-- SOLO LOCAL. Vistas de estructura del portal (sección 4.1) sobre el esquema simulado
-- SISTEMA_OT. En producción se crean con docs/oracle/vistas_produccion.sql, apuntando a las
-- tablas reales: mismos nombres de vista y mismas columnas, para que el código no cambie.
-- Llevan sufijo _pgpm porque son objetos que crea el portal.
-- =====================================================================================

-- Tipos de reporte de disponibilidad y sus umbrales de semáforo (Paso 1).
CREATE OR REPLACE VIEW PGPM.VW_UMBRALES_SEMAFORO_PGPM AS
SELECT TIPO_REPORTE, DESCRIPCION, UMBRAL_ROJO, UMBRAL_NARANJA
FROM   SISTEMA_OT.OT_TIPO_REPORTE;

-- Familias y su ventana de reparación por programa (Paso 3).
CREATE OR REPLACE VIEW PGPM.VW_ESTRUCTURA_FAMILIAS_PGPM AS
SELECT ANIO_PROGRAMA, FAMILIA_CODIGO, FAMILIA_NOMBRE, FECHA_EST_INICIO, FECHA_EST_FIN, CANT_EQUIPO
FROM   SISTEMA_OT.OT_FAMILIA_DEFINIDA;

-- Curva de avance planeado por familia y semana (Paso 3, "Semana Definida Por Familia").
-- La spec no traía FAMILIA_CODIGO en esta vista, pero la curva es por familia.
CREATE OR REPLACE VIEW PGPM.VW_CURVA_PLANEADA_PGPM AS
SELECT ANIO_PROGRAMA, FAMILIA_CODIGO, MES, NO_SEMANA, FECHA_INICIO_SEMANA, FECHA_FIN_SEMANA,
       ACTIVIDAD_PCT, DIAS
FROM   SISTEMA_OT.OT_SEMANA_FAMILIA;

-- Equipos requeridos por familia y asignación (Paso 4).
CREATE OR REPLACE VIEW PGPM.VW_EQUIPOS_REQUERIDOS_PGPM AS
SELECT TIPO_REPORTE, FAMILIA_CODIGO, ASIGNACION, CANT_ASIGNADA, CANT_REQUERIDA
FROM   SISTEMA_OT.OT_EQUIPOS_REQUERIDOS;

-- Equipos y su familia (no estaba en la spec): sin esto no hay cruce SAP <-> familia por
-- código legado, ni detalle de equipos por familia (sección 8.2).
CREATE OR REPLACE VIEW PGPM.VW_EQUIPOS_FAMILIA_PGPM AS
SELECT CODIGO_LEGADO, DESCRIPCION, FAMILIA_CODIGO, ACTIVO
FROM   SISTEMA_OT.OT_EQUIPO;
