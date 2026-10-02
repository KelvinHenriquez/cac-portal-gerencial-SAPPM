-- =====================================================================================
-- SOLO LOCAL. Simula las tablas de configuración del módulo "Sistema de Órdenes de Trabajo"
-- de Oracle Forms, con la forma de sus pantallas (capturas del 01/10/2026):
--   MA_CONFIGURACION_DISP -> reportes de disponibilidad, sus semanas, horas y asignaciones.
--   MA_CONFIG_AVANCE      -> períodos, reportes de avance, familias y plan semanal.
-- Los nombres OT_* son inventados: los reales están pendientes. Lo que importa es que las
-- vistas VW_*_PGPM (03_vistas_pgpm.sql) exponen las columnas que el portal espera; en
-- producción esas vistas se apuntan a las tablas reales (docs/oracle/vistas_produccion.sql).
-- Compatible con Oracle 11g.
-- =====================================================================================
ALTER SESSION SET CURRENT_SCHEMA = SISTEMA_OT;

-- ------------------------------------------------------------------ equipos (Ficha)
-- CODIGO_LEGADO es el código viejo que SAP también trae cargado: la llave de cruce.
-- En Oracle cada equipo tiene dos códigos: Ficha (F01168) y Equipo (A29017-002). Aquí
-- CODIGO_LEGADO hace de Ficha; pendiente confirmar cuál de los dos trae SAP como legado.
CREATE TABLE OT_EQUIPO (
    CODIGO_LEGADO   VARCHAR2(30)  PRIMARY KEY,
    EQUIPO_CODIGO   VARCHAR2(30)  NOT NULL,
    DESCRIPCION     VARCHAR2(200) NOT NULL,
    FAMILIA_CODIGO  VARCHAR2(60)  NOT NULL,
    ACTIVO          CHAR(1)       DEFAULT 'S' NOT NULL
);

-- ------------------------------------------------------------------ disponibilidad
-- Paso 1: tipo de reporte con sus dos umbrales (rojo si < COMP1, naranja si < COMP2).
CREATE TABLE OT_DISP_REPORTE (
    REPORTE_ID   NUMBER(10)    PRIMARY KEY,
    ORDEN        NUMBER(10)    NOT NULL,
    DESCRIPCION  VARCHAR2(200) NOT NULL,
    COMP1        NUMBER(5,2)   NOT NULL,
    COMP2        NUMBER(5,2)   NOT NULL,
    ESTADO       VARCHAR2(10)  DEFAULT 'Activo' NOT NULL
);

-- Paso 2: semanas del reporte, generadas a partir de un rango de fechas.
CREATE TABLE OT_DISP_SEMANA (
    REPORTE_ID    NUMBER(10) NOT NULL,
    NO_SEMANA     NUMBER(3)  NOT NULL,
    MES           NUMBER(2)  NOT NULL,
    FECHA_INICIO  DATE       NOT NULL,
    FECHA_FIN     DATE       NOT NULL,
    PRIMARY KEY (REPORTE_ID, NO_SEMANA)
);

-- Paso 3: horas del día en que corre el proceso (en que se toma la foto de disponibilidad).
CREATE TABLE OT_DISP_HORA (
    REPORTE_ID  NUMBER(10)   NOT NULL,
    HORA        NUMBER(2)    NOT NULL,
    ESTADO      VARCHAR2(10) DEFAULT 'Activo' NOT NULL,
    PRIMARY KEY (REPORTE_ID, HORA)
);

-- Paso 4: asignaciones (filas del reporte) con sus requeridos y su meta.
CREATE TABLE OT_DISP_ASIGNACION (
    REPORTE_ID      NUMBER(10)    NOT NULL,
    ASIGNACION_ID   NUMBER(10)    NOT NULL,
    ORDEN           NUMBER(10)    NOT NULL,
    ASIGNACION      VARCHAR2(200) NOT NULL,
    CANT_ASIG       NUMBER(10)    NOT NULL,
    CANT_REQ        NUMBER(10)    NOT NULL,
    DISP_COMP       NUMBER(5,2)   NOT NULL,
    NEGRITA         VARCHAR2(2)   DEFAULT 'No' NOT NULL,
    GRUPO           VARCHAR2(2)   DEFAULT 'No' NOT NULL,
    PRIMARY KEY (REPORTE_ID, ASIGNACION_ID)
);

-- Botón "..." de Cant. Asig.: los equipos de cada asignación (por grupo o directos; aquí ya
-- resueltos a equipos individuales).
CREATE TABLE OT_DISP_ASIG_EQUIPO (
    REPORTE_ID     NUMBER(10)   NOT NULL,
    ASIGNACION_ID  NUMBER(10)   NOT NULL,
    CODIGO_LEGADO  VARCHAR2(30) NOT NULL,
    PRIMARY KEY (REPORTE_ID, ASIGNACION_ID, CODIGO_LEGADO)
);

-- ------------------------------------------------------------------ avance
CREATE TABLE OT_PERIODO (
    PERIODO_ID   NUMBER(10)    PRIMARY KEY,
    DESCRIPCION  VARCHAR2(100) NOT NULL
);

-- Paso 1 de "Configuración Reporte Avance": cada reporte pertenece a un período.
CREATE TABLE OT_AVANCE_REPORTE (
    REPORTE_ID    NUMBER(10)    PRIMARY KEY,
    PERIODO_ID    NUMBER(10)    NOT NULL,
    TIPO_OT       VARCHAR2(60)  NOT NULL,
    FECHA_REF     VARCHAR2(30)  NOT NULL,
    NOMBRE        VARCHAR2(200) NOT NULL,
    FECHA_INICIO  DATE          NOT NULL,
    FECHA_FIN     DATE          NOT NULL
);

-- Paso 3: familias del reporte.
CREATE TABLE OT_AVANCE_FAMILIA (
    REPORTE_ID        NUMBER(10)    NOT NULL,
    ORDEN             NUMBER(10)    NOT NULL,
    FAMILIA_CODIGO    VARCHAR2(60)  NOT NULL,
    FAMILIA_NOMBRE    VARCHAR2(200) NOT NULL,
    FECHA_EST_INICIO  DATE          NOT NULL,
    FECHA_EST_FIN     DATE          NOT NULL,
    CANT_EQUIPO       NUMBER(10)    NOT NULL,
    PRIMARY KEY (REPORTE_ID, FAMILIA_CODIGO)
);

-- "Órdenes de Trabajo Asignada a Familia": las OT (y por lo tanto los equipos) que forman el
-- programa de esa familia en ese reporte. Una misma familia puede estar en varios reportes con
-- equipos distintos (Tractores: 14 en "Varios", 3 en "Nivelación").
CREATE TABLE OT_AVANCE_OT (
    REPORTE_ID      NUMBER(10)   NOT NULL,
    FAMILIA_CODIGO  VARCHAR2(60) NOT NULL,
    NO_OT           VARCHAR2(20) NOT NULL,
    CODIGO_LEGADO   VARCHAR2(30) NOT NULL,
    CANT_ACTIVIDAD  NUMBER(10,2) NOT NULL,
    CANT_DIAS       NUMBER(10,2) NOT NULL,
    PRIMARY KEY (REPORTE_ID, FAMILIA_CODIGO, NO_OT)
);

-- "Semana Definida Por Familia": actividades y días planificados por semana.
CREATE TABLE OT_AVANCE_SEMANA (
    REPORTE_ID      NUMBER(10)   NOT NULL,
    FAMILIA_CODIGO  VARCHAR2(60) NOT NULL,
    NO_SEMANA       NUMBER(3)    NOT NULL,
    MES             NUMBER(2)    NOT NULL,
    FECHA_INICIO    DATE         NOT NULL,
    FECHA_FIN       DATE         NOT NULL,
    ACTIVIDAD       NUMBER(10,2) NOT NULL,
    DIAS            NUMBER(10,2) NOT NULL,
    PRIMARY KEY (REPORTE_ID, FAMILIA_CODIGO, NO_SEMANA)
);

-- ================================================================== datos de prueba

-- Equipos: maquinaria del taller (con programa de reparación) y equipos de riego.
DECLARE
    PROCEDURE equipos(p_prefijo VARCHAR2, p_cant NUMBER, p_desc VARCHAR2, p_familia VARCHAR2) IS
    BEGIN
        FOR i IN 1 .. p_cant LOOP
            INSERT INTO OT_EQUIPO VALUES (p_prefijo || LPAD(i, 3, '0'),
                'A' || TO_CHAR(29000 + ASCII(SUBSTR(p_prefijo, 1, 1)) * 10 + ASCII(SUBSTR(p_prefijo, 2, 1))) || '-' || LPAD(i, 3, '0'),
                p_desc || ' #' || i, p_familia, 'S');
        END LOOP;
    END;
BEGIN
    equipos('TR-', 45,  'Tractor agrícola',     'TRACTORES');
    equipos('VG-', 120, 'Vagón cañero',         'VAGONES');
    equipos('CM-', 30,  'Camión de tiro',       'CAMIONES');
    equipos('CO-', 12,  'Cosechadora de caña',  'COSECHADORAS');
    equipos('AL-', 18,  'Alzadora de caña',     'ALZADORAS');
    equipos('MN-', 6,   'Motoniveladora',       'MOTONIVELADORAS');
    equipos('RE-', 8,   'Retroexcavadora',      'RETROEXCAVADORAS');
    equipos('BD-', 5,   'Tractor de oruga',     'BULLDOZERS');
    equipos('RB-', 10,  'Bomba de riego',       'BOMBAS_RIEGO');
    equipos('RG-', 11,  'Bomba de goteo',       'BOMBAS_RIEGO');
    equipos('RA-', 10,  'Bomba de aspersión',   'BOMBAS_RIEGO');
    equipos('RC-', 24,  'Bomba de caudal',      'BOMBAS_RIEGO');
    equipos('RP-', 3,   'Pozo',                 'POZOS');
    equipos('RT-', 6,   'Turbina vertical',     'TURBINAS');
END;
/
UPDATE OT_EQUIPO SET ACTIVO = 'N' WHERE CODIGO_LEGADO IN ('TR-045', 'VG-120', 'RC-024');

-- Reportes de disponibilidad, como en la captura de configuración.
INSERT INTO OT_DISP_REPORTE VALUES (51, 1, 'Equipos De Riegos - Gerencial',     80, 85, 'Activo');
INSERT INTO OT_DISP_REPORTE VALUES (52, 2, 'Equipos De Riegos - Detallado P30', 80, 85, 'Activo');
INSERT INTO OT_DISP_REPORTE VALUES (53, 3, 'Centro De Servicios - P30',         80, 85, 'Activo');
INSERT INTO OT_DISP_REPORTE VALUES (54, 4, 'Maquinaria De Nivelación',          70, 80, 'Activo');

-- Semanas (Paso 2): rango 01/06/2026 al 27/12/2026, lunes a domingo. Horas (Paso 3): todas,
-- salvo el detallado de riego, que solo mide de 6:00 a 18:00.
DECLARE
    v_lunes DATE;
BEGIN
    FOR r IN (SELECT REPORTE_ID FROM OT_DISP_REPORTE) LOOP
        v_lunes := DATE '2026-06-01';
        FOR s IN 1 .. 30 LOOP
            INSERT INTO OT_DISP_SEMANA VALUES (r.REPORTE_ID, s, EXTRACT(MONTH FROM v_lunes), v_lunes, v_lunes + 6);
            v_lunes := v_lunes + 7;
        END LOOP;
        FOR h IN 0 .. 23 LOOP
            INSERT INTO OT_DISP_HORA VALUES (r.REPORTE_ID, h,
                CASE WHEN r.REPORTE_ID = 52 AND (h < 6 OR h > 18) THEN 'Inactivo' ELSE 'Activo' END);
        END LOOP;
    END LOOP;
END;
/

-- Asignaciones (Paso 4) y sus equipos.
DECLARE
    v_id NUMBER := 1;
    PROCEDURE asig(p_rep NUMBER, p_id NUMBER, p_nombre VARCHAR2, p_prefijo VARCHAR2, p_desde NUMBER, p_hasta NUMBER,
                   p_req NUMBER, p_comp NUMBER DEFAULT 100, p_grupo VARCHAR2 DEFAULT 'No') IS
        v_cant NUMBER := 0;
    BEGIN
        FOR i IN p_desde .. p_hasta LOOP
            INSERT INTO OT_DISP_ASIG_EQUIPO VALUES (p_rep, p_id, p_prefijo || LPAD(i, 3, '0'));
            v_cant := v_cant + 1;
        END LOOP;
        -- Una fila de grupo va en negrita, como en la configuración real.
        INSERT INTO OT_DISP_ASIGNACION VALUES (p_rep, p_id, p_id, p_nombre, v_cant, p_req, p_comp, p_grupo, p_grupo);
    END;

    -- Reporte "detallado": el grupo y, debajo, cada uno de sus equipos como fila propia (nombrada
    -- por su ficha), como "Aguacatico Caudal" seguido de F01163, F01196... en la captura.
    PROCEDURE grupo(p_rep NUMBER, p_id IN OUT NUMBER, p_nombre VARCHAR2, p_prefijo VARCHAR2, p_cant NUMBER, p_req NUMBER) IS
    BEGIN
        asig(p_rep, p_id, p_nombre, p_prefijo, 1, p_cant, p_req, 100, 'Si');
        FOR i IN 1 .. p_cant LOOP
            p_id := p_id + 1;
            asig(p_rep, p_id, p_prefijo || LPAD(i, 3, '0'), p_prefijo, i, i, 1);
        END LOOP;
        p_id := p_id + 1;
    END;
BEGIN
    asig(51, 1, 'Aguacatico Caudal',    'RB-', 1, 10, 10);
    asig(51, 2, 'Aguacatico Goteo',     'RG-', 1, 11, 11);
    asig(51, 3, 'Aspersión Bombas',     'RA-', 1, 10, 9);
    asig(51, 4, 'Caudal',               'RC-', 1, 24, 23);
    asig(51, 5, 'Pozos Finca Madre',    'RP-', 1, 3, 3);
    asig(51, 6, 'Turbinas Verticales',  'RT-', 1, 6, 6);

    grupo(52, v_id, 'Aguacatico Caudal', 'RB-', 10, 10);
    grupo(52, v_id, 'Aguacatico Goteo',  'RG-', 11, 11);

    asig(53, 1, 'Tractores Tiro',       'TR-', 1, 28, 26);
    asig(53, 2, 'Tractores Cultivo',    'TR-', 29, 40, 10);
    asig(53, 3, 'Vagones',              'VG-', 1, 120, 105);
    asig(53, 4, 'Camiones De Tiro',     'CM-', 1, 30, 25);
    asig(53, 5, 'Cosechadoras',         'CO-', 1, 12, 10, 95);
    asig(53, 6, 'Alzadoras',            'AL-', 1, 18, 15);

    asig(54, 1, 'Motoniveladoras',      'MN-', 1, 6, 5);
    asig(54, 2, 'Retroexcavadoras',     'RE-', 1, 8, 6);
    asig(54, 3, 'Bulldozer',            'BD-', 1, 5, 4);
    asig(54, 4, 'Tractores Nivelación', 'TR-', 41, 45, 4);
END;
/

-- Períodos y reportes de avance, como en la captura de configuración.
INSERT INTO OT_PERIODO VALUES (29, '2024 - 2025');
INSERT INTO OT_PERIODO VALUES (30, '2025 - 2026');

INSERT INTO OT_AVANCE_REPORTE VALUES (1, 29, 'Programa Reparación', 'Estimada', 'Programa De Reparacion 2025',            DATE '2025-06-30', DATE '2025-12-07');
INSERT INTO OT_AVANCE_REPORTE VALUES (2, 30, 'Programa Reparación', 'Estimada', 'Programa De Reparacion 2026',            DATE '2026-06-01', DATE '2026-12-13');
INSERT INTO OT_AVANCE_REPORTE VALUES (3, 30, 'Programa Reparación', 'Estimada', 'Programa De Reparacion 2026 Nivelacion', DATE '2026-07-06', DATE '2026-12-06');

INSERT INTO OT_AVANCE_FAMILIA VALUES (1, 1, 'TRACTORES',        'Tractores',         DATE '2025-06-30', DATE '2025-11-28', 45);
INSERT INTO OT_AVANCE_FAMILIA VALUES (1, 2, 'VAGONES',          'Vagones',           DATE '2025-07-07', DATE '2025-12-05', 120);
INSERT INTO OT_AVANCE_FAMILIA VALUES (2, 1, 'COSECHADORAS',     'Cosechadoras',      DATE '2026-06-01', DATE '2026-11-20', 12);
INSERT INTO OT_AVANCE_FAMILIA VALUES (2, 2, 'TRACTORES',        'Tractores',         DATE '2026-06-01', DATE '2026-11-27', 40);
INSERT INTO OT_AVANCE_FAMILIA VALUES (2, 3, 'CAMIONES',         'Camiones De Tiro',  DATE '2026-06-08', DATE '2026-12-04', 30);
INSERT INTO OT_AVANCE_FAMILIA VALUES (2, 4, 'VAGONES',          'Vagones',           DATE '2026-06-15', DATE '2026-12-10', 120);
INSERT INTO OT_AVANCE_FAMILIA VALUES (2, 5, 'ALZADORAS',        'Alzadoras',         DATE '2026-06-22', DATE '2026-12-10', 18);
INSERT INTO OT_AVANCE_FAMILIA VALUES (3, 1, 'MOTONIVELADORAS',  'Motoniveladoras',   DATE '2026-07-06', DATE '2026-11-13', 6);
INSERT INTO OT_AVANCE_FAMILIA VALUES (3, 2, 'RETROEXCAVADORAS', 'Retroexcavadoras',  DATE '2026-07-06', DATE '2026-11-27', 8);
INSERT INTO OT_AVANCE_FAMILIA VALUES (3, 3, 'BULLDOZERS',       'Bulldozer',         DATE '2026-07-13', DATE '2026-12-04', 5);
INSERT INTO OT_AVANCE_FAMILIA VALUES (3, 4, 'TRACTORES',        'Tractores',         DATE '2026-07-20', DATE '2026-11-20', 5);

-- OT asignadas a cada familia: una por equipo activo. Tractores se reparte entre dos reportes
-- (TR-001..040 en el programa general, TR-041..045 en Nivelación).
DECLARE
    v_n NUMBER := 0;
BEGIN
    FOR f IN (SELECT REPORTE_ID, FAMILIA_CODIGO FROM OT_AVANCE_FAMILIA) LOOP
        FOR e IN (SELECT CODIGO_LEGADO FROM OT_EQUIPO
                  WHERE FAMILIA_CODIGO = f.FAMILIA_CODIGO AND ACTIVO = 'S'
                    AND (f.FAMILIA_CODIGO <> 'TRACTORES'
                         OR (f.REPORTE_ID = 3 AND CODIGO_LEGADO >= 'TR-041')
                         OR (f.REPORTE_ID <> 3 AND CODIGO_LEGADO < 'TR-041'))
                  ORDER BY CODIGO_LEGADO) LOOP
            v_n := v_n + 1;
            INSERT INTO OT_AVANCE_OT VALUES (f.REPORTE_ID, f.FAMILIA_CODIGO,
                CASE f.REPORTE_ID WHEN 1 THEN '29' ELSE '30' END || 'CPR-' || LPAD(v_n, 5, '0'), e.CODIGO_LEGADO, 20, 1.8);
        END LOOP;
    END LOOP;
    -- Cant. Equipo = las OT asignadas, como en la pantalla de configuración.
    UPDATE OT_AVANCE_FAMILIA f SET CANT_EQUIPO =
        (SELECT COUNT(*) FROM OT_AVANCE_OT o WHERE o.REPORTE_ID = f.REPORTE_ID AND o.FAMILIA_CODIGO = f.FAMILIA_CODIGO);
END;
/

-- Plan semanal por familia en "S": semanas del calendario del reporte (lunes a domingo) que
-- caen en la ventana de la familia; actividades = ~20 por equipo repartidas con un peso seno;
-- días ≈ 0.09 por actividad (como 550 actividades / 48.8 días en la captura).
DECLARE
    v_inicio   DATE;
    v_semanas  PLS_INTEGER;
    v_total    NUMBER;
    v_act_tot  NUMBER;
    v_act      NUMBER;
    v_acum     NUMBER;
    v_ini_sem  DATE;
BEGIN
    FOR f IN (SELECT f.*, r.FECHA_INICIO AS REP_INICIO FROM OT_AVANCE_FAMILIA f JOIN OT_AVANCE_REPORTE r ON r.REPORTE_ID = f.REPORTE_ID) LOOP
        v_inicio  := TRUNC(f.FECHA_EST_INICIO, 'IW');
        v_semanas := CEIL((TRUNC(f.FECHA_EST_FIN, 'IW') - v_inicio) / 7) + 1;
        v_act_tot := f.CANT_EQUIPO * 20;
        v_total   := 0;
        FOR i IN 1 .. v_semanas LOOP
            v_total := v_total + SIN(ACOS(-1) * (i - 0.5) / v_semanas);
        END LOOP;
        v_acum := 0;
        FOR i IN 1 .. v_semanas LOOP
            IF i < v_semanas THEN
                v_act := ROUND(v_act_tot * SIN(ACOS(-1) * (i - 0.5) / v_semanas) / v_total);
            ELSE
                v_act := v_act_tot - v_acum;
            END IF;
            v_acum := v_acum + v_act;
            v_ini_sem := v_inicio + (i - 1) * 7;
            INSERT INTO OT_AVANCE_SEMANA VALUES (
                f.REPORTE_ID, f.FAMILIA_CODIGO,
                -- número de semana dentro del calendario del reporte
                (v_ini_sem - TRUNC(f.REP_INICIO, 'IW')) / 7 + 1,
                EXTRACT(MONTH FROM v_ini_sem), v_ini_sem, v_ini_sem + 6,
                v_act, ROUND(v_act * 0.09, 2));
        END LOOP;
    END LOOP;
END;
/

COMMIT;

-- El usuario técnico del portal lee estas tablas a través de sus vistas _pgpm (GRANT directo,
-- no por rol, para poder crear vistas encima).
BEGIN
    FOR t IN (SELECT TABLE_NAME FROM ALL_TABLES WHERE OWNER = 'SISTEMA_OT') LOOP
        EXECUTE IMMEDIATE 'GRANT SELECT ON SISTEMA_OT.' || t.TABLE_NAME || ' TO PGPM';
    END LOOP;
END;
/
