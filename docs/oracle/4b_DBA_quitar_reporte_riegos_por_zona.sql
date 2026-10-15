-- =====================================================================================
-- Quita el reporte "Equipos De Riegos - Por Zona" de las 5 tablas de configuración de
-- disponibilidad de MAQUINARIAN (lo que creó 4_DBA_reporte_riegos_por_zona.sql). No toca nada
-- más. SQL Developer, F5. Una sola transacción.
-- =====================================================================================
SET SERVEROUTPUT ON
DECLARE
  v_tipo NUMBER;
BEGIN
  SELECT ID_TIPO_REPORTE INTO v_tipo FROM MAQUINARIAN.MA_CONFIG_DISPONIBILIDAD
   WHERE ID_EMPRESA = 30 AND ID_AREA_SERVICIO = 13 AND DESCRIPCION = 'Equipos De Riegos - Por Zona';
  DELETE FROM MAQUINARIAN.MA_DEQUIPOS_REQ_X_FAMILIA WHERE ID_EMPRESA = 30 AND ID_AREA_SERVICIO = 13 AND ID_TIPO_REPORTE = v_tipo;
  DELETE FROM MAQUINARIAN.MA_EQUIPOS_REQ_X_FAMILIA  WHERE ID_EMPRESA = 30 AND ID_AREA_SERVICIO = 13 AND ID_TIPO_REPORTE = v_tipo;
  DELETE FROM MAQUINARIAN.MA_CONFIG_SEMANA_DISP     WHERE ID_EMPRESA = 30 AND ID_AREA_SERVICIO = 13 AND ID_TIPO_REPORTE = v_tipo;
  DELETE FROM MAQUINARIAN.MA_CONFIG_HORA_DISP       WHERE ID_EMPRESA = 30 AND ID_AREA_SERVICIO = 13 AND ID_TIPO_REPORTE = v_tipo;
  DELETE FROM MAQUINARIAN.MA_CONFIG_DISPONIBILIDAD  WHERE ID_EMPRESA = 30 AND ID_AREA_SERVICIO = 13 AND ID_TIPO_REPORTE = v_tipo;
  COMMIT;
  DBMS_OUTPUT.PUT_LINE('Quitado el reporte ' || v_tipo || '.');
EXCEPTION
  WHEN NO_DATA_FOUND THEN DBMS_OUTPUT.PUT_LINE('No existe el reporte: no se hizo nada.');
  WHEN TOO_MANY_ROWS THEN ROLLBACK; RAISE_APPLICATION_ERROR(-20003, 'Hay más de un reporte con ese nombre: revisar a mano.');
  WHEN OTHERS THEN ROLLBACK; RAISE;
END;
/
