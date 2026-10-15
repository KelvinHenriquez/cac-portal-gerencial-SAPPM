-- =====================================================================================
-- PRODUCCIÓN — Vistas de estructura del Portal Gerencial SAP PM (Oracle 11g).
-- Lo corre el DBA en el esquema del portal (F5 en SQL Developer), una vez y cada vez que cambie
-- una tabla de origen. Es idempotente (CREATE OR REPLACE).
--
-- Escritas sobre las tablas reales del esquema MAQUINARIAN (diccionario exportado el 02/10/2026:
-- docs/oracle/1.xlsx a 5.xlsx). NO cambiar los nombres de vista ni los alias de columna: son el
-- contrato con el portal (la versión local, sobre el esquema simulado SISTEMA_OT, está en
-- db/oracle/init/03_vistas_pgpm.sql).
--
-- Antes de correrlo:
--   1. La empresa de CAC es la 30 (explorar_maquinarian_2.sql, sección 6). SQL Developer la pide
--      una sola vez al correr (variable ID_EMPRESA); también se puede fijar antes con
--      DEFINE ID_EMPRESA = 30.
--   2. Ajustado el 03/10/2026 con la salida de explorar_maquinarian_2.sql y el PL/SQL de Oracle
--      (PR_DISPONIBILIDAD_EQUIPOS, PR_ACTUALIZAR_CONFIG_AVANCE): mismas exclusiones que Oracle.
--   3. El dueño de las vistas necesita GRANT SELECT directo (no por rol); como MAQUINARIAN o DBA:
--        GRANT SELECT ON MAQUINARIAN.MA_MAESTRO_EQUIPOS        TO <esquema_portal>;
--        GRANT SELECT ON MAQUINARIAN.MA_CONFIG_DISPONIBILIDAD  TO <esquema_portal>;
--        GRANT SELECT ON MAQUINARIAN.MA_CONFIG_SEMANA_DISP     TO <esquema_portal>;
--        GRANT SELECT ON MAQUINARIAN.MA_CONFIG_HORA_DISP       TO <esquema_portal>;
--        GRANT SELECT ON MAQUINARIAN.MA_EQUIPOS_REQ_X_FAMILIA  TO <esquema_portal>;
--        GRANT SELECT ON MAQUINARIAN.MA_DEQUIPOS_REQ_X_FAMILIA TO <esquema_portal>;
--        GRANT SELECT ON MAQUINARIAN.MA_ENC_CONFIG_AVANCE      TO <esquema_portal>;
--        GRANT SELECT ON MAQUINARIAN.MA_DET_CONFIG_AVANCE      TO <esquema_portal>;
--        GRANT SELECT ON MAQUINARIAN.MA_SUBDET_CONFIG_AVANCE   TO <esquema_portal>;
--        GRANT SELECT ON MAQUINARIAN.MA_OT_CONFIG_AVANCE       TO <esquema_portal>;
--        GRANT SELECT ON MAQUINARIAN.MA_ENC_SEMANA_OT          TO <esquema_portal>;
--        GRANT SELECT ON MAQUINARIAN.MA_DET_SEMANA_OT          TO <esquema_portal>;
--        GRANT SELECT ON MAQUINARIAN.MA_TIPO_ORDEN_TRABAJO     TO <esquema_portal>;
--        GRANT SELECT ON MAQUINARIAN.MA_MST_FAMILIA            TO <esquema_portal>;
--        GRANT SELECT ON PARGER.PG_PERIODO                     TO <esquema_portal>;
--
-- Llaves: en Oracle Forms los reportes de disponibilidad se identifican por
-- (empresa, área de servicio, tipo de reporte); el portal usa un solo número:
-- REPORTE_ID = ID_AREA_SERVICIO * 1000 + ID_TIPO_REPORTE.
-- =====================================================================================
SET DEFINE ON

-- Equipos (Ficha) con su familia: cruce SAP <-> Oracle por la Ficha, que SAP guarda como código
-- legado en FLEET_NUM (KEY_NUM en Z_PM_MF_WS_MAESTRO_EQUIPO). ESTADO es ACTIVO / INACTIVO.
-- Hay ~50 fichas repetidas en el maestro: se queda una por ficha, la activa primero. Las fichas
-- terminadas en "000" (C01000, G01000...) no son equipos: Oracle las excluye del conteo.
CREATE OR REPLACE VIEW VW_EQUIPOS_FAMILIA_PGPM AS
SELECT CODIGO_LEGADO, EQUIPO_CODIGO, DESCRIPCION, FAMILIA_CODIGO, ACTIVO
FROM  (SELECT e.FICHA                AS CODIGO_LEGADO,   -- "F01168"
              e.EQUIPO               AS EQUIPO_CODIGO,   -- "A29017-002"
              e.DESCRIPCION          AS DESCRIPCION,
              TO_CHAR(e.ID_FAMILIA)  AS FAMILIA_CODIGO,
              CASE WHEN UPPER(e.ESTADO) = 'ACTIVO' THEN 'S' ELSE 'N' END AS ACTIVO,
              ROW_NUMBER() OVER (PARTITION BY e.FICHA
                                 ORDER BY CASE WHEN UPPER(e.ESTADO) = 'ACTIVO' THEN 0 ELSE 1 END, e.IDENTIFICADOR DESC) AS N
       FROM   MAQUINARIAN.MA_MAESTRO_EQUIPOS e
       WHERE  e.ID_EMPRESA = &&ID_EMPRESA
       AND    e.FICHA IS NOT NULL
       AND    SUBSTR(e.FICHA, 4, 3) <> '000')
WHERE  N = 1;

-- =============================================================== disponibilidad

-- Paso 1: tipo de reporte. Rojo si < Comp. 1; naranja si < Comp. 2.
CREATE OR REPLACE VIEW VW_DISP_REPORTE_PGPM AS
SELECT c.ID_AREA_SERVICIO * 1000 + c.ID_TIPO_REPORTE AS REPORTE_ID,
       NVL(c.ORDEN, c.ID_TIPO_REPORTE)                AS ORDEN,
       c.DESCRIPCION                                  AS DESCRIPCION,
       NVL(c.VALOR_COMP, 0)                           AS UMBRAL_ROJO,
       NVL(c.VALOR_COMP2, 0)                          AS UMBRAL_NARANJA,
       CASE WHEN UPPER(c.ESTADO) = 'ACTIVO' THEN 'S' ELSE 'N' END AS ACTIVO
FROM   MAQUINARIAN.MA_CONFIG_DISPONIBILIDAD c
WHERE  c.ID_EMPRESA = &&ID_EMPRESA;

-- Paso 2: semanas del reporte (NO_SEMANA no se repite dentro de un reporte: sección 8).
CREATE OR REPLACE VIEW VW_DISP_SEMANA_PGPM AS
SELECT s.ID_AREA_SERVICIO * 1000 + s.ID_TIPO_REPORTE AS REPORTE_ID,
       s.NO_SEMANA, s.ID_MES AS MES, s.FECHA_INICIO, s.FECHA_FIN
FROM   MAQUINARIAN.MA_CONFIG_SEMANA_DISP s
WHERE  s.ID_EMPRESA = &&ID_EMPRESA;

-- Paso 3: horas del día en que se mide (0 a 23). HORA es texto de 24 h con un espacio delante
-- (' 00:00' ... ' 23:00', sección 8).
CREATE OR REPLACE VIEW VW_DISP_HORA_PGPM AS
SELECT h.ID_AREA_SERVICIO * 1000 + h.ID_TIPO_REPORTE AS REPORTE_ID,
       TO_NUMBER(SUBSTR(TRIM(h.HORA), 1, INSTR(TRIM(h.HORA), ':') - 1)) AS HORA,
       CASE WHEN UPPER(NVL(h.ESTADO, 'ACTIVO')) = 'ACTIVO' THEN 'S' ELSE 'N' END AS ACTIVO
FROM   MAQUINARIAN.MA_CONFIG_HORA_DISP h
WHERE  h.ID_EMPRESA = &&ID_EMPRESA;

-- Paso 4: asignaciones (filas del reporte). Un grupo junta equipos de distinta naturaleza que
-- juntos generan su disponibilidad. El portal toma como "incluida en el grupo" a la fila que no es
-- grupo y cuyos equipos están todos en un grupo del mismo reporte (no importa la posición): se
-- muestra, pero en "Todas" solo cuenta el grupo.
CREATE OR REPLACE VIEW VW_DISP_ASIGNACION_PGPM AS
SELECT a.ID_AREA_SERVICIO * 1000 + a.ID_TIPO_REPORTE AS REPORTE_ID,
       a.CORRELATIVO                                  AS ASIGNACION_ID,
       NVL(a.ORDEN, a.CORRELATIVO)                    AS ORDEN,
       a.DESCRIPCION                                  AS ASIGNACION,
       NVL(a.CANT, 0)                                 AS CANT_ASIGNADA,
       NVL(a.CANT_REQ, 0)                             AS CANT_REQUERIDA,
       NVL(a.DISP_COMP, 0)                            AS DISP_COMP_PCT,
       CASE WHEN UPPER(a.IND_NEGRITA) IN ('SI', 'S') THEN 'S' ELSE 'N' END AS NEGRITA,
       CASE WHEN UPPER(a.IND_GRUPO)   IN ('SI', 'S') THEN 'S' ELSE 'N' END AS GRUPO
FROM   MAQUINARIAN.MA_EQUIPOS_REQ_X_FAMILIA a
WHERE  a.ID_EMPRESA = &&ID_EMPRESA
-- Oracle no mide las filas sin requeridos
AND    NVL(a.CANT_REQ, 0) <> 0
-- ni las que no tienen ningún equipo activo (PR_DISPONIBILIDAD_EQUIPOS: CANT_EQ = 0 no se inserta)
AND    EXISTS (SELECT 1
               FROM   MAQUINARIAN.MA_DEQUIPOS_REQ_X_FAMILIA d
               JOIN   MAQUINARIAN.MA_MAESTRO_EQUIPOS e
                      ON e.ID_EMPRESA = d.ID_EMPRESA AND e.CODIGO_EQUIPO = d.CODIGO_EQUIPO AND e.IDENTIFICADOR = d.IDENTIFICADOR
               WHERE  d.ID_EMPRESA = a.ID_EMPRESA AND d.ID_AREA_SERVICIO = a.ID_AREA_SERVICIO
               AND    d.ID_TIPO_REPORTE = a.ID_TIPO_REPORTE AND d.CORRELATIVO = a.CORRELATIVO
               AND    SUBSTR(NVL(d.FICHA, '00000'), 4, 3) <> '000'
               AND    UPPER(e.ESTADO) = 'ACTIVO');

-- Equipos de cada asignación ("Detalle de Equipos Asignados"). Como Oracle: solo los equipos
-- ACTIVOS, mirando el registro exacto del maestro (CODIGO_EQUIPO + IDENTIFICADOR), no la ficha:
-- con fichas repetidas, una puede estar activa y la otra no. La ficha se toma del maestro.
CREATE OR REPLACE VIEW VW_DISP_ASIG_EQUIPO_PGPM AS
SELECT d.ID_AREA_SERVICIO * 1000 + d.ID_TIPO_REPORTE AS REPORTE_ID,
       d.CORRELATIVO                                  AS ASIGNACION_ID,
       NVL(e.FICHA, d.FICHA)                          AS CODIGO_LEGADO
FROM   MAQUINARIAN.MA_DEQUIPOS_REQ_X_FAMILIA d
JOIN   MAQUINARIAN.MA_MAESTRO_EQUIPOS e
       ON e.ID_EMPRESA = d.ID_EMPRESA AND e.CODIGO_EQUIPO = d.CODIGO_EQUIPO AND e.IDENTIFICADOR = d.IDENTIFICADOR
WHERE  d.ID_EMPRESA = &&ID_EMPRESA
AND    UPPER(e.ESTADO) = 'ACTIVO'
AND    NVL(e.FICHA, d.FICHA) IS NOT NULL
-- misma exclusión que PR_DISPONIBILIDAD_EQUIPOS
AND    SUBSTR(NVL(d.FICHA, '00000'), 4, 3) <> '000';

-- =============================================================== avance

-- Período de reparación ("30: 2025-2026"). La tabla es de PARGER (respuesta del 03/10/2026).
-- Columnas confirmadas el 06/10/2026 (ID_EMPRESA, ID_PERIODO, DESCRIPCION, fechas).
CREATE OR REPLACE VIEW VW_AVANCE_PERIODO_PGPM AS
SELECT p.ID_PERIODO AS PERIODO_ID, p.DESCRIPCION AS DESCRIPCION
FROM   PARGER.PG_PERIODO p
WHERE  p.ID_EMPRESA = &&ID_EMPRESA
AND    p.ID_PERIODO IN (SELECT ID_PERIODO FROM MAQUINARIAN.MA_ENC_CONFIG_AVANCE WHERE ID_EMPRESA = &&ID_EMPRESA);

-- Reporte de avance ("Programa de Reparación 2026", ...) con las fechas de su calendario.
CREATE OR REPLACE VIEW VW_AVANCE_REPORTE_PGPM AS
SELECT e.CORRELATIVO               AS REPORTE_ID,
       e.ID_PERIODO                AS PERIODO_ID,
       e.CONCEPTO                  AS NOMBRE,
       NVL(t.DESCRIPCION, TO_CHAR(e.ID_TIPO_OT)) AS TIPO_OT,
       NVL(e.FECHA_TRABAJAR, ' ')  AS FECHA_REF,
       s.FECHA_INICIO              AS FECHA_INICIO,
       s.FECHA_FIN                 AS FECHA_FIN
FROM   MAQUINARIAN.MA_ENC_CONFIG_AVANCE e
JOIN   MAQUINARIAN.MA_ENC_SEMANA_OT s
       ON s.ID_EMPRESA = e.ID_EMPRESA AND s.ID_PERIODO = e.ID_PERIODO
      AND s.ID_AREA_SERVICIO = e.ID_AREA AND s.CORRELATIVO = e.ID_TIPO_SEMANA
LEFT JOIN MAQUINARIAN.MA_TIPO_ORDEN_TRABAJO t
       ON t.ID_EMPRESA = e.ID_EMPRESA AND t.ID_TIPO_OT = e.ID_TIPO_OT
WHERE  e.ID_EMPRESA = &&ID_EMPRESA;

-- Familias del reporte. FAMILIA_CODIGO = familia-correlativo: una familia puede repetirse en un
-- programa (Molinos dos veces en el Programa de Reparación 2021). Ojo: aquí ID_FAMILIA es del maestro MA_MST_FAMILIA (como en
-- PR_RES_CONFIG_SEMANA), no del MA_FAMILIA por área que usa el maestro de equipos: los equipos
-- de cada familia salen de las OT asignadas (VW_AVANCE_OT_PGPM), no del código de familia.
CREATE OR REPLACE VIEW VW_AVANCE_FAMILIA_PGPM AS
SELECT d.CORRELATIVO                                  AS REPORTE_ID,
       NVL(d.ORDEN, d.DCORRELATIVO)                   AS ORDEN,
       TO_CHAR(d.ID_FAMILIA) || '-' || TO_CHAR(d.DCORRELATIVO)                          AS FAMILIA_CODIGO,
       NVL(INITCAP(f.DESCRIPCION), d.DESCRIPCION)     AS FAMILIA_NOMBRE,
       NVL(d.FECHA_EST_INICIO, s.FECHA_INICIO)        AS FECHA_EST_INICIO,
       NVL(d.FECHA_EST_FIN, s.FECHA_FIN)              AS FECHA_EST_FIN,
       NVL(d.CANT_EQUIPO, 0)                          AS CANT_EQUIPO
FROM   MAQUINARIAN.MA_DET_CONFIG_AVANCE d
JOIN   MAQUINARIAN.MA_ENC_CONFIG_AVANCE e ON e.ID_EMPRESA = d.ID_EMPRESA AND e.CORRELATIVO = d.CORRELATIVO
JOIN   MAQUINARIAN.MA_ENC_SEMANA_OT s
       ON s.ID_EMPRESA = e.ID_EMPRESA AND s.ID_PERIODO = e.ID_PERIODO
      AND s.ID_AREA_SERVICIO = e.ID_AREA AND s.CORRELATIVO = e.ID_TIPO_SEMANA
LEFT JOIN MAQUINARIAN.MA_MST_FAMILIA f ON f.ID_EMPRESA = d.ID_EMPRESA AND f.ID_FAMILIA = d.ID_FAMILIA
WHERE  d.ID_EMPRESA = &&ID_EMPRESA;

-- "Órdenes de Trabajo Asignada a Familia": MA_OT_CONFIG_AVANCE tiene una fila por actividad de
-- cada OT; aquí se resume a una fila por OT con su equipo, actividades y días. El portal cruza con
-- SAP por la ficha del equipo (el número de OT es el de Oracle). Actividades y días: solo las
-- marcadas SELECCIONADO = 'S', como cuenta Oracle el real programado.
CREATE OR REPLACE VIEW VW_AVANCE_OT_PGPM AS
SELECT o.CORRELATIVO                    AS REPORTE_ID,
       TO_CHAR(d.ID_FAMILIA) || '-' || TO_CHAR(d.DCORRELATIVO)            AS FAMILIA_CODIGO,
       TO_CHAR(o.NO_ORDEN_TRABAJO)      AS NO_OT,
       MAX(e.FICHA)                     AS CODIGO_LEGADO,
       COUNT(CASE WHEN o.SELECCIONADO = 'S' THEN 1 END)                     AS CANT_ACTIVIDAD,
       NVL(SUM(CASE WHEN o.SELECCIONADO = 'S' THEN o.DIAS_ESTIMADOS END), 0) AS CANT_DIAS,
       -- Semana de la última actividad planificada: entrega programada del equipo (15/10/2026).
       MAX(CASE WHEN o.SELECCIONADO = 'S' THEN o.NO_SEMANA END)           AS SEMANA_FIN
FROM   MAQUINARIAN.MA_OT_CONFIG_AVANCE o
JOIN   MAQUINARIAN.MA_DET_CONFIG_AVANCE d
       ON d.ID_EMPRESA = o.ID_EMPRESA AND d.CORRELATIVO = o.CORRELATIVO AND d.DCORRELATIVO = o.DCORRELATIVO
JOIN   MAQUINARIAN.MA_MAESTRO_EQUIPOS e ON e.IDENTIFICADOR = o.IDENTIFICADOR_EQUIPO
WHERE  o.ID_EMPRESA = &&ID_EMPRESA
AND    e.FICHA IS NOT NULL
GROUP BY o.CORRELATIVO, d.ID_FAMILIA, d.DCORRELATIVO, o.NO_ORDEN_TRABAJO;

-- "Semana Definida Por Familia": actividades y días planificados de cada semana (no acumulados),
-- con las fechas del calendario del reporte.
CREATE OR REPLACE VIEW VW_AVANCE_SEMANA_PGPM AS
SELECT sd.CORRELATIVO                  AS REPORTE_ID,
       TO_CHAR(d.ID_FAMILIA) || '-' || TO_CHAR(d.DCORRELATIVO)           AS FAMILIA_CODIGO,
       sd.NO_SEMANA                    AS NO_SEMANA,
       sd.ID_MES                       AS MES,
       w.FECHA_INICIO                  AS FECHA_INICIO,
       w.FECHA_FIN                     AS FECHA_FIN,
       NVL(sd.CANT_ACTIVIDAD, 0)       AS ACTIVIDAD,
       NVL(sd.CANT_DIA_ACT, 0)         AS DIAS
FROM   MAQUINARIAN.MA_SUBDET_CONFIG_AVANCE sd
JOIN   MAQUINARIAN.MA_DET_CONFIG_AVANCE d
       ON d.ID_EMPRESA = sd.ID_EMPRESA AND d.CORRELATIVO = sd.CORRELATIVO AND d.DCORRELATIVO = sd.DCORRELATIVO
JOIN   MAQUINARIAN.MA_ENC_CONFIG_AVANCE e ON e.ID_EMPRESA = d.ID_EMPRESA AND e.CORRELATIVO = d.CORRELATIVO
JOIN   MAQUINARIAN.MA_DET_SEMANA_OT w
       ON w.ID_EMPRESA = e.ID_EMPRESA AND w.ID_PERIODO = sd.ID_PERIODO
      AND w.ID_AREA_SERVICIO = e.ID_AREA AND w.CORRELATIVO = e.ID_TIPO_SEMANA
      AND w.ID_MES = sd.ID_MES AND w.NO_SEMANA = sd.NO_SEMANA
WHERE  sd.ID_EMPRESA = &&ID_EMPRESA;
