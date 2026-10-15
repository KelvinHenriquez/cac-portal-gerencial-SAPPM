-- =====================================================================================
-- PRODUCCIÓN — Reporte de disponibilidad "Equipos De Riegos - Por Zona (Resumen)" (15/10/2026).
-- Crea en Oracle Forms (MAQUINARIAN) lo mismo que se haría por las pantallas de configuración:
-- el reporte, sus horas, su calendario y UNA FILA POR ZONA con todas sus motobombas (sin el
-- detalle por bomba: versión resumida del reporte "Por Zona", como el 51 "Gerencial").
-- Origen: "Indices RPM CAC Julio 26.xlsx", hoja "Revisado-150726" (5 zonas, 73 equipos).
--
-- Lo corre un usuario con permiso de INSERT en las tablas MA_* de MAQUINARIAN (SQL Developer, F5).
-- Es seguro de correr:
--   - Si ya existe un reporte con este nombre, no hace nada y lo dice.
--   - Busca cada Ficha en el maestro AL CORRER (registro ACTIVO); si alguna falta, no crea nada.
--   - Todo va en una sola transacción: o queda todo, o no queda nada.
-- Copia del reporte 52 (Equipos De Riegos - Detallado P30): umbrales, las 24 horas y las 52
-- semanas (03/11/2025 al 01/11/2026). Fuera: J08022 y J08026 (plantas eléctricas inactivas).
-- Para quitarlo: 5b_DBA_quitar_reporte_riegos_por_zona_resumen.sql
-- El portal lo muestra en Disponibilidad después de la siguiente actualización.
-- =====================================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
SET DEFINE OFF

DECLARE
  c_empresa CONSTANT NUMBER := 30;
  c_area    CONSTANT NUMBER := 13;
  c_base    CONSTANT NUMBER := 52;   -- Equipos De Riegos - Detallado P30
  c_nombre  CONSTANT VARCHAR2(200) := 'Equipos De Riegos - Por Zona (Resumen)';

  TYPE t_bomba IS RECORD (zona NUMBER, ficha VARCHAR2(30), supervisor VARCHAR2(150),
                          identificador NUMBER, codigo NUMBER, equipo VARCHAR2(100), familia NUMBER);
  TYPE t_bombas IS TABLE OF t_bomba INDEX BY PLS_INTEGER;
  v_bombas  t_bombas;
  v_n       PLS_INTEGER := 0;
  v_usr     VARCHAR2(100) := SUBSTR(USER || '-' || TO_CHAR(SYSDATE, 'DD/MM/YYYY HH24:MI'), 1, 100);
  v_tipo    NUMBER;
  v_corr    NUMBER := 0;
  v_cuenta  NUMBER;
  v_faltan  VARCHAR2(4000);

  PROCEDURE agregar(p_zona NUMBER, p_ficha VARCHAR2, p_supervisor VARCHAR2) IS
  BEGIN
    v_n := v_n + 1;
    v_bombas(v_n).zona := p_zona;
    v_bombas(v_n).ficha := p_ficha;
    v_bombas(v_n).supervisor := p_supervisor;
  END;

  PROCEDURE equipo_en_fila(p_corr NUMBER, i PLS_INTEGER) IS
  BEGIN
    INSERT INTO MAQUINARIAN.MA_DEQUIPOS_REQ_X_FAMILIA
      (ID_EMPRESA, ID_AREA_SERVICIO, ID_TIPO_REPORTE, ID_FAMILIA, CODIGO_EQUIPO, IDENTIFICADOR, EQUIPO, FICHA, USRGRAB, CORRELATIVO)
    VALUES
      (c_empresa, c_area, v_tipo, v_bombas(i).familia, v_bombas(i).codigo, v_bombas(i).identificador,
       v_bombas(i).equipo, v_bombas(i).ficha, v_usr, p_corr);
  END;

  PROCEDURE fila(p_desc VARCHAR2, p_req NUMBER, p_grupo BOOLEAN) IS
    v_ind VARCHAR2(1) := CASE WHEN p_grupo THEN 'S' ELSE 'N' END;  -- en SQL de 11g no hay BOOLEAN
  BEGIN
    v_corr := v_corr + 1;
    INSERT INTO MAQUINARIAN.MA_EQUIPOS_REQ_X_FAMILIA
      (ID_EMPRESA, ID_FAMILIA, CANT_REQ, USRGRAB, DISP_COMP, ID_AREA_SERVICIO, ID_TIPO_REPORTE, CANT,
       DESCRIPCION, ORDEN, CORRELATIVO, IND_NEGRITA, IND_GRUPO)
    VALUES
      (c_empresa, -1, p_req, v_usr, 100, c_area, v_tipo, NULL,
       SUBSTR(p_desc, 1, 200), v_corr, v_corr,
       v_ind, v_ind);
  END;
BEGIN
  -- Zona, Ficha, Supervisor (en el orden del Excel)
  agregar(1, 'F01186', 'Felix Rafael De Leon Novas');
  agregar(1, 'F01215', 'Felix Rafael De Leon Novas');
  agregar(1, 'F01066', 'Ignacio Medina Celina');
  agregar(1, 'F01156', 'Joselin Castro Mendez');
  agregar(1, 'F01197', 'Joselin Castro Mendez');
  agregar(1, 'F01191', 'Joselin Castro Mendez');
  agregar(1, 'F01192', 'Joselin Castro Mendez');
  agregar(1, 'F01196', 'Joselin Castro Mendez');
  agregar(1, 'F01168', 'Joselin Castro Mendez');
  agregar(1, 'F01170', 'Joselin Castro Mendez');
  agregar(1, 'F01155', 'Lisandro Alberto Perez Diaz');
  agregar(1, 'F01104', 'Lisandro Alberto Perez Diaz');
  agregar(1, 'F01163', 'Lisandro Alberto Perez Diaz');
  agregar(1, 'F01161', 'Lisandro Alberto Perez Diaz');
  agregar(1, 'F01162', 'Lisandro Alberto Perez Diaz');
  agregar(1, 'F01218', 'Lisandro Alberto Perez Diaz');
  agregar(1, 'F01169', 'Lisandro Alberto Perez Diaz');
  agregar(1, 'F01137', 'Wirton Reyes Matos');
  agregar(1, 'F01138', 'Wirton Reyes Matos');
  agregar(1, 'F01139', 'Wirton Reyes Matos');
  agregar(2, 'F01153', 'Elcilio Mesa Gomez');
  agregar(2, 'F01187', 'Elcilio Mesa Gomez');
  agregar(2, 'F01217', 'Elcilio Mesa Gomez');
  agregar(2, 'F01101', 'Lizardo Alberto Feliz Urbaez');
  agregar(2, 'F01132', 'Lizardo Alberto Feliz Urbaez');
  agregar(2, 'F01177', 'Lizardo Alberto Feliz Urbaez');
  agregar(2, 'F01210', 'Lizardo Alberto Feliz Urbaez');
  agregar(2, 'F01091', 'Rolando Campo Ramirez');
  agregar(2, 'F01107', 'Rolando Campo Ramirez');
  agregar(2, 'F01176', 'Rolando Campo Ramirez');
  agregar(2, 'F01208', 'Rolando Campo Ramirez');
  agregar(2, 'F01209', 'Rolando Campo Ramirez');
  agregar(2, 'F01216', 'Rolando Campo Ramirez');
  agregar(2, 'F01223', 'Rolando Campo Ramirez');
  agregar(3, 'F01133', 'Daniel Alberto Reyes Vicente');
  agregar(3, 'F01172', 'Euberto Antuan Luis');
  agregar(3, 'F01173', 'Euberto Antuan Luis');
  agregar(3, 'F01174', 'Euberto Antuan Luis');
  agregar(3, 'F01206', 'Euberto Antuan Luis');
  agregar(3, 'F01205', 'Euberto Antuan Luis');
  agregar(3, 'F01207', 'Euberto Antuan Luis');
  agregar(3, 'F01106', 'Jose Miguel Tejeda Martinez');
  agregar(3, 'F01123', 'Jose Miguel Tejeda Martinez');
  agregar(3, 'F01124', 'Jose Miguel Tejeda Martinez');
  agregar(3, 'F01164', 'Jose Miguel Tejeda Martinez');
  agregar(3, 'F01089', 'Jose Miguel Tejeda Martinez');
  agregar(3, 'F01212', 'Jose Miguel Tejeda Martinez');
  agregar(4, 'F01017', 'Franklin Robelin Feliz Feliz');
  agregar(4, 'F01129', 'Franklin Robelin Feliz Feliz');
  agregar(4, 'F01188', 'Franklin Robelin Feliz Feliz');
  agregar(4, 'F01211', 'Franklin Robelin Feliz Feliz');
  agregar(4, 'F01213', 'Franklin Robelin Feliz Feliz');
  agregar(4, 'F01125', 'Jean Thonio Syprien');
  agregar(4, 'F01185', 'Jean Thonio Syprien');
  agregar(4, 'F01214', 'Jean Thonio Syprien');
  agregar(4, 'F01166', 'Manuel Antonio Martinez Vasquez');
  agregar(4, 'F01178', 'Manuel Antonio Martinez Vasquez');
  agregar(6, 'F01220', 'Fernando Rodriguez Perez');
  agregar(6, 'F01181', 'Fernando Rodriguez Perez');
  agregar(6, 'F01096', 'Fernando Rodriguez Perez');
  agregar(6, 'F01193', 'Fernando Rodriguez Perez');
  agregar(6, 'F02003', 'Fernando Rodriguez Perez');
  agregar(6, 'F02004', 'Fernando Rodriguez Perez');
  agregar(6, 'F01056', 'Fernando Rodriguez Perez');
  agregar(6, 'F01112', 'Fernando Rodriguez Perez');
  agregar(6, 'F02005', 'Fernando Rodriguez Perez');
  agregar(6, 'F01115', 'Fernando Rodriguez Perez');
  agregar(6, 'F01135', 'Fernando Rodriguez Perez');
  agregar(6, 'F01195', 'Fernando Rodriguez Perez');
  agregar(6, 'F01200', 'Fernando Rodriguez Perez');
  agregar(6, 'F01194', 'Fernando Rodriguez Perez');
  agregar(6, 'F01108', 'Fernando Rodriguez Perez');
  agregar(6, 'F01203', 'Fernando Rodriguez Perez');

  -- 1. No crear dos veces.
  SELECT COUNT(*) INTO v_cuenta FROM MAQUINARIAN.MA_CONFIG_DISPONIBILIDAD
   WHERE ID_EMPRESA = c_empresa AND UPPER(TRIM(DESCRIPCION)) = UPPER(c_nombre);
  IF v_cuenta > 0 THEN
    DBMS_OUTPUT.PUT_LINE('Ya existe un reporte "' || c_nombre || '": no se hizo nada.');
    RETURN;
  END IF;

  SELECT COUNT(*) INTO v_cuenta FROM MAQUINARIAN.MA_CONFIG_DISPONIBILIDAD
   WHERE ID_EMPRESA = c_empresa AND ID_AREA_SERVICIO = c_area AND ID_TIPO_REPORTE = c_base;
  IF v_cuenta = 0 THEN
    RAISE_APPLICATION_ERROR(-20001, 'No existe el reporte base ' || c_base || ' (Riegos - Detallado P30).');
  END IF;

  -- 2. Cada Ficha en el maestro (el registro ACTIVO; si hubiera dos, el más reciente).
  FOR i IN 1 .. v_n LOOP
    BEGIN
      SELECT IDENTIFICADOR, CODIGO_EQUIPO, EQUIPO, ID_FAMILIA
        INTO v_bombas(i).identificador, v_bombas(i).codigo, v_bombas(i).equipo, v_bombas(i).familia
        FROM (SELECT IDENTIFICADOR, CODIGO_EQUIPO, EQUIPO, ID_FAMILIA
                FROM MAQUINARIAN.MA_MAESTRO_EQUIPOS
               WHERE ID_EMPRESA = c_empresa AND FICHA = v_bombas(i).ficha AND UPPER(ESTADO) = 'ACTIVO'
               ORDER BY IDENTIFICADOR DESC)
       WHERE ROWNUM = 1;
    EXCEPTION WHEN NO_DATA_FOUND THEN
      v_faltan := SUBSTR(v_faltan || ' ' || v_bombas(i).ficha, 1, 3900);
    END;
  END LOOP;
  IF v_faltan IS NOT NULL THEN
    RAISE_APPLICATION_ERROR(-20002, 'Fichas sin registro ACTIVO en el maestro (no se creó nada):' || v_faltan);
  END IF;

  -- 3. Número del reporte: el siguiente libre del área (mirando también horas y semanas).
  SELECT NVL(MAX(t), 0) + 1 INTO v_tipo FROM (
    SELECT MAX(ID_TIPO_REPORTE) t FROM MAQUINARIAN.MA_CONFIG_DISPONIBILIDAD WHERE ID_EMPRESA = c_empresa AND ID_AREA_SERVICIO = c_area
    UNION ALL SELECT MAX(ID_TIPO_REPORTE) FROM MAQUINARIAN.MA_CONFIG_HORA_DISP WHERE ID_EMPRESA = c_empresa AND ID_AREA_SERVICIO = c_area
    UNION ALL SELECT MAX(ID_TIPO_REPORTE) FROM MAQUINARIAN.MA_CONFIG_SEMANA_DISP WHERE ID_EMPRESA = c_empresa AND ID_AREA_SERVICIO = c_area
    UNION ALL SELECT MAX(ID_TIPO_REPORTE) FROM MAQUINARIAN.MA_EQUIPOS_REQ_X_FAMILIA WHERE ID_EMPRESA = c_empresa AND ID_AREA_SERVICIO = c_area);

  -- 4. Reporte, horas y semanas (copiados del 52).
  INSERT INTO MAQUINARIAN.MA_CONFIG_DISPONIBILIDAD
    (ID_EMPRESA, ID_TIPO_REPORTE, DESCRIPCION, ESTADO, USRGRAB, VALOR_COMP, VALOR_COMP2, ID_AREA_SERVICIO, ORDEN)
  SELECT c_empresa, v_tipo, c_nombre, 'ACTIVO', v_usr, VALOR_COMP, VALOR_COMP2, c_area, v_tipo
    FROM MAQUINARIAN.MA_CONFIG_DISPONIBILIDAD
   WHERE ID_EMPRESA = c_empresa AND ID_AREA_SERVICIO = c_area AND ID_TIPO_REPORTE = c_base;

  INSERT INTO MAQUINARIAN.MA_CONFIG_HORA_DISP (ID_EMPRESA, ID_TIPO_REPORTE, HORA, ORDEN, ESTADO, USRGRAB, ID_AREA_SERVICIO)
  SELECT c_empresa, v_tipo, HORA, ORDEN, ESTADO, v_usr, c_area
    FROM MAQUINARIAN.MA_CONFIG_HORA_DISP
   WHERE ID_EMPRESA = c_empresa AND ID_AREA_SERVICIO = c_area AND ID_TIPO_REPORTE = c_base;

  INSERT INTO MAQUINARIAN.MA_CONFIG_SEMANA_DISP (ID_EMPRESA, ID_TIPO_REPORTE, ID_MES, NO_SEMANA, FECHA_INICIO, FECHA_FIN, ORDEN, USRGRAB, ID_AREA_SERVICIO)
  SELECT c_empresa, v_tipo, ID_MES, NO_SEMANA, FECHA_INICIO, FECHA_FIN, ORDEN, v_usr, c_area
    FROM MAQUINARIAN.MA_CONFIG_SEMANA_DISP
   WHERE ID_EMPRESA = c_empresa AND ID_AREA_SERVICIO = c_area AND ID_TIPO_REPORTE = c_base;

  -- 5. Una fila por zona con todas sus bombas.
  FOR z IN (SELECT COLUMN_VALUE zona FROM TABLE(SYS.ODCINUMBERLIST(1, 2, 3, 4, 6))) LOOP
    v_cuenta := 0;
    FOR i IN 1 .. v_n LOOP
      IF v_bombas(i).zona = z.zona THEN v_cuenta := v_cuenta + 1; END IF;
    END LOOP;
    fila('Zona ' || z.zona, v_cuenta, FALSE);
    FOR i IN 1 .. v_n LOOP
      IF v_bombas(i).zona = z.zona THEN equipo_en_fila(v_corr, i); END IF;
    END LOOP;
  END LOOP;

  COMMIT;
  DBMS_OUTPUT.PUT_LINE('Creado el reporte ' || v_tipo || ' "' || c_nombre || '" (área de servicio ' || c_area || '): '
                       || v_corr || ' filas, ' || v_n || ' equipos.');
EXCEPTION WHEN OTHERS THEN
  ROLLBACK;
  RAISE;
END;
/

PROMPT ===== Verificación: filas del reporte nuevo =====
SELECT r.ORDEN, r.DESCRIPCION, r.CANT_REQ, r.IND_GRUPO,
       (SELECT COUNT(*) FROM MAQUINARIAN.MA_DEQUIPOS_REQ_X_FAMILIA d
         WHERE d.ID_EMPRESA = r.ID_EMPRESA AND d.ID_AREA_SERVICIO = r.ID_AREA_SERVICIO
           AND d.ID_TIPO_REPORTE = r.ID_TIPO_REPORTE AND d.CORRELATIVO = r.CORRELATIVO) AS EQUIPOS
  FROM MAQUINARIAN.MA_EQUIPOS_REQ_X_FAMILIA r
  JOIN MAQUINARIAN.MA_CONFIG_DISPONIBILIDAD c
    ON c.ID_EMPRESA = r.ID_EMPRESA AND c.ID_AREA_SERVICIO = r.ID_AREA_SERVICIO AND c.ID_TIPO_REPORTE = r.ID_TIPO_REPORTE
 WHERE c.ID_EMPRESA = 30 AND c.DESCRIPCION = 'Equipos De Riegos - Por Zona (Resumen)'
 ORDER BY r.ORDEN;
