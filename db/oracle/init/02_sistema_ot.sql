-- =====================================================================================
-- SOLO LOCAL. Simula las tablas de configuración del módulo "Sistema de Órdenes de Trabajo"
-- de Oracle Forms (sección 4.1). Los nombres OT_* son inventados: los reales están pendientes
-- (sección 13). Lo que importa es que las vistas VW_*_PGPM (03_vistas_pgpm.sql) exponen
-- exactamente las columnas que el portal espera; en producción esas mismas vistas se apuntan a
-- las tablas reales (docs/oracle/vistas_produccion.sql) y el código del portal no cambia.
--
-- Datos de prueba: Programa de Reparación 2026 (interzafra), del 1/6/2026 al 10/12/2026.
-- =====================================================================================
ALTER SESSION SET CONTAINER = FREEPDB1;
ALTER SESSION SET CURRENT_SCHEMA = SISTEMA_OT;

-- Tipos de reporte de disponibilidad + umbrales de semáforo (Paso 1 de "Configuración
-- Reporte Disponibilidad"). Disponibilidad < UMBRAL_ROJO = rojo; < UMBRAL_NARANJA = naranja;
-- si no, verde.
CREATE TABLE OT_TIPO_REPORTE (
    TIPO_REPORTE    VARCHAR2(60)  PRIMARY KEY,
    DESCRIPCION     VARCHAR2(200) NOT NULL,
    UMBRAL_ROJO     NUMBER(5,2)   NOT NULL,
    UMBRAL_NARANJA  NUMBER(5,2)   NOT NULL
);

-- Familias de equipos y su ventana de reparación por programa (Paso 3 de "Configuración
-- Reporte Avance OT").
CREATE TABLE OT_FAMILIA_DEFINIDA (
    ANIO_PROGRAMA     VARCHAR2(20)  NOT NULL,
    FAMILIA_CODIGO    VARCHAR2(60)  NOT NULL,
    FAMILIA_NOMBRE    VARCHAR2(200) NOT NULL,
    FECHA_EST_INICIO  DATE          NOT NULL,
    FECHA_EST_FIN     DATE          NOT NULL,
    CANT_EQUIPO       NUMBER(10)    NOT NULL,
    PRIMARY KEY (ANIO_PROGRAMA, FAMILIA_CODIGO)
);

-- Maestro de equipos (Objeto Técnico / Ficha) con su familia. CODIGO_LEGADO es el código
-- viejo que SAP también trae cargado: es la llave de cruce SAP <-> Oracle (sección 3).
CREATE TABLE OT_EQUIPO (
    CODIGO_LEGADO   VARCHAR2(30)  PRIMARY KEY,
    DESCRIPCION     VARCHAR2(200) NOT NULL,
    FAMILIA_CODIGO  VARCHAR2(60)  NOT NULL,
    ACTIVO          CHAR(1)       DEFAULT 'S' NOT NULL
);

-- Equipos requeridos por familia y asignación/proceso productivo (Paso 4 de "Configuración
-- Reporte Disponibilidad").
CREATE TABLE OT_EQUIPOS_REQUERIDOS (
    TIPO_REPORTE    VARCHAR2(60) NOT NULL,
    FAMILIA_CODIGO  VARCHAR2(60) NOT NULL,
    ASIGNACION      VARCHAR2(60) NOT NULL,
    CANT_ASIGNADA   NUMBER(10)   NOT NULL,
    CANT_REQUERIDA  NUMBER(10)   NOT NULL,
    PRIMARY KEY (TIPO_REPORTE, FAMILIA_CODIGO, ASIGNACION)
);

-- Curva de avance planeado: % de la actividad total que se programa en cada semana, por
-- familia ("Semana Definida Por Familia", Paso 3).
CREATE TABLE OT_SEMANA_FAMILIA (
    ANIO_PROGRAMA        VARCHAR2(20) NOT NULL,
    FAMILIA_CODIGO       VARCHAR2(60) NOT NULL,
    NO_SEMANA            NUMBER(3)    NOT NULL,
    MES                  NUMBER(2)    NOT NULL,
    FECHA_INICIO_SEMANA  DATE         NOT NULL,
    FECHA_FIN_SEMANA     DATE         NOT NULL,
    ACTIVIDAD_PCT        NUMBER(5,2)  NOT NULL,
    DIAS                 NUMBER(2)    NOT NULL,
    PRIMARY KEY (ANIO_PROGRAMA, FAMILIA_CODIGO, NO_SEMANA)
);

INSERT INTO OT_TIPO_REPORTE VALUES ('CS-P30',     'Centro de Servicios - P30',    70, 85);
INSERT INTO OT_TIPO_REPORTE VALUES ('NIVELACION', 'Maquinaria de Nivelación',     60, 80);

INSERT INTO OT_FAMILIA_DEFINIDA VALUES ('2026', 'TRACTORES',       'Tractores agrícolas',       DATE '2026-06-01', DATE '2026-11-27',  45);
INSERT INTO OT_FAMILIA_DEFINIDA VALUES ('2026', 'VAGONES',         'Vagones cañeros',           DATE '2026-06-15', DATE '2026-12-10', 120);
INSERT INTO OT_FAMILIA_DEFINIDA VALUES ('2026', 'CAMIONES',        'Camiones de tiro',          DATE '2026-06-08', DATE '2026-12-04',  30);
INSERT INTO OT_FAMILIA_DEFINIDA VALUES ('2026', 'COSECHADORAS',    'Cosechadoras de caña',      DATE '2026-06-01', DATE '2026-11-20',  12);
INSERT INTO OT_FAMILIA_DEFINIDA VALUES ('2026', 'ALZADORAS',       'Alzadoras de caña',         DATE '2026-06-22', DATE '2026-12-10',  18);
INSERT INTO OT_FAMILIA_DEFINIDA VALUES ('2026', 'MOTONIVELADORAS', 'Motoniveladoras',           DATE '2026-07-06', DATE '2026-11-13',   6);
INSERT INTO OT_FAMILIA_DEFINIDA VALUES ('2026', 'RETROEXCAVADORAS','Retroexcavadoras',          DATE '2026-07-06', DATE '2026-11-27',   8);
INSERT INTO OT_FAMILIA_DEFINIDA VALUES ('2026', 'BULLDOZERS',      'Tractores de oruga',        DATE '2026-07-13', DATE '2026-12-04',   5);

-- Equipos: código legado con prefijo por familia (TR-001, VG-001, ...). Uno de cada familia
-- grande queda inactivo para que el cálculo tenga que respetar ACTIVO.
BEGIN
    FOR f IN (SELECT FAMILIA_CODIGO, FAMILIA_NOMBRE, CANT_EQUIPO FROM OT_FAMILIA_DEFINIDA WHERE ANIO_PROGRAMA = '2026') LOOP
        FOR i IN 1 .. f.CANT_EQUIPO LOOP
            INSERT INTO OT_EQUIPO (CODIGO_LEGADO, DESCRIPCION, FAMILIA_CODIGO, ACTIVO)
            VALUES (
                CASE f.FAMILIA_CODIGO
                    WHEN 'TRACTORES'        THEN 'TR-'
                    WHEN 'VAGONES'          THEN 'VG-'
                    WHEN 'CAMIONES'         THEN 'CM-'
                    WHEN 'COSECHADORAS'     THEN 'CO-'
                    WHEN 'ALZADORAS'        THEN 'AL-'
                    WHEN 'MOTONIVELADORAS'  THEN 'MN-'
                    WHEN 'RETROEXCAVADORAS' THEN 'RE-'
                    WHEN 'BULLDOZERS'       THEN 'BD-'
                END || LPAD(i, 3, '0'),
                RTRIM(f.FAMILIA_NOMBRE, 's') || ' #' || i,
                f.FAMILIA_CODIGO,
                'S');
        END LOOP;
    END LOOP;
END;
/
UPDATE OT_EQUIPO SET ACTIVO = 'N' WHERE CODIGO_LEGADO IN ('TR-045', 'VG-120');

INSERT INTO OT_EQUIPOS_REQUERIDOS VALUES ('CS-P30', 'TRACTORES',    'TIRO',      28, 26);
INSERT INTO OT_EQUIPOS_REQUERIDOS VALUES ('CS-P30', 'TRACTORES',    'CULTIVO',   12, 10);
INSERT INTO OT_EQUIPOS_REQUERIDOS VALUES ('CS-P30', 'VAGONES',      'TIRO',     118, 105);
INSERT INTO OT_EQUIPOS_REQUERIDOS VALUES ('CS-P30', 'CAMIONES',     'TIRO',      30, 25);
INSERT INTO OT_EQUIPOS_REQUERIDOS VALUES ('CS-P30', 'COSECHADORAS', 'CORTE',     12, 10);
INSERT INTO OT_EQUIPOS_REQUERIDOS VALUES ('CS-P30', 'ALZADORAS',    'ALCE',      18, 15);
INSERT INTO OT_EQUIPOS_REQUERIDOS VALUES ('NIVELACION', 'MOTONIVELADORAS',  'NIVELACION', 6, 5);
INSERT INTO OT_EQUIPOS_REQUERIDOS VALUES ('NIVELACION', 'RETROEXCAVADORAS', 'NIVELACION', 8, 6);
INSERT INTO OT_EQUIPOS_REQUERIDOS VALUES ('NIVELACION', 'BULLDOZERS',       'NIVELACION', 5, 4);
INSERT INTO OT_EQUIPOS_REQUERIDOS VALUES ('NIVELACION', 'TRACTORES',        'NIVELACION', 5, 4);

-- Curva en "S": semanas lunes-domingo desde el lunes de la semana de inicio hasta la de fin,
-- con un peso seno por semana (poco al principio y al final, mucho en el medio). Los % suman
-- exactamente 100 por familia (la última semana absorbe el redondeo).
DECLARE
    v_inicio  DATE;
    v_semanas PLS_INTEGER;
    v_total   NUMBER;
    v_pct     NUMBER;
    v_acum    NUMBER;
    v_ini_sem DATE;
    v_fin_sem DATE;
    v_dias    PLS_INTEGER;
BEGIN
    FOR f IN (SELECT FAMILIA_CODIGO, FECHA_EST_INICIO, FECHA_EST_FIN FROM OT_FAMILIA_DEFINIDA WHERE ANIO_PROGRAMA = '2026') LOOP
        v_inicio  := TRUNC(f.FECHA_EST_INICIO, 'IW');
        v_semanas := CEIL((TRUNC(f.FECHA_EST_FIN, 'IW') - v_inicio) / 7) + 1;
        v_total   := 0;
        FOR i IN 1 .. v_semanas LOOP
            v_total := v_total + SIN(ACOS(-1) * (i - 0.5) / v_semanas);
        END LOOP;

        v_acum := 0;
        FOR i IN 1 .. v_semanas LOOP
            IF i < v_semanas THEN
                v_pct := ROUND(100 * SIN(ACOS(-1) * (i - 0.5) / v_semanas) / v_total, 2);
            ELSE
                v_pct := 100 - v_acum;
            END IF;
            v_acum := v_acum + v_pct;

            v_ini_sem := GREATEST(v_inicio + (i - 1) * 7, f.FECHA_EST_INICIO);
            v_fin_sem := LEAST(v_inicio + (i - 1) * 7 + 6, f.FECHA_EST_FIN);
            -- Días laborables (lunes a sábado) dentro de la semana recortada.
            v_dias := 0;
            FOR d IN 0 .. (v_fin_sem - v_ini_sem) LOOP
                IF TO_CHAR(v_ini_sem + d, 'DY', 'NLS_DATE_LANGUAGE=ENGLISH') <> 'SUN' THEN
                    v_dias := v_dias + 1;
                END IF;
            END LOOP;

            INSERT INTO OT_SEMANA_FAMILIA
            VALUES ('2026', f.FAMILIA_CODIGO, i, EXTRACT(MONTH FROM v_ini_sem), v_ini_sem, v_fin_sem, v_pct, v_dias);
        END LOOP;
    END LOOP;
END;
/

COMMIT;

-- El usuario técnico del portal lee estas tablas a través de sus vistas _pgpm. El GRANT debe
-- ser directo (no por rol) para poder crear vistas encima.
GRANT SELECT ON SISTEMA_OT.OT_TIPO_REPORTE       TO PGPM;
GRANT SELECT ON SISTEMA_OT.OT_FAMILIA_DEFINIDA   TO PGPM;
GRANT SELECT ON SISTEMA_OT.OT_EQUIPO             TO PGPM;
GRANT SELECT ON SISTEMA_OT.OT_EQUIPOS_REQUERIDOS TO PGPM;
GRANT SELECT ON SISTEMA_OT.OT_SEMANA_FAMILIA     TO PGPM;
