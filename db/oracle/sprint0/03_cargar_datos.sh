#!/bin/bash
# SOLO SPRINT 0. Carga en la réplica de MAQUINARIAN los exports de configuración que dejó el DBA
# en sprint0/oracle/*.sql (SQL Developer > Herramientas > Exportar base de datos, formato INSERT).
# Corre en orden alfabético; un error en una fila no detiene el resto (queda en el log del contenedor).
shopt -s nullglob
archivos=(/opt/pgpm/datos/*.sql)
if [ ${#archivos[@]} -eq 0 ]; then
  echo "SPRINT 0: no hay exports de configuración en sprint0/oracle/: la réplica queda vacía."
  exit 0
fi
for f in "${archivos[@]}"; do
  echo "SPRINT 0: cargando $f"
  sqlplus -s / as sysdba <<SQL
SET DEFINE OFF
SET FEEDBACK OFF
ALTER SESSION SET CURRENT_SCHEMA = MAQUINARIAN;
ALTER SESSION SET NLS_DATE_FORMAT = 'DD/MM/RR';
ALTER SESSION SET NLS_DATE_LANGUAGE = 'AMERICAN';
@"$f"
COMMIT;
EXIT
SQL
done
sqlplus -s / as sysdba <<'SQL'
SET PAGESIZE 100 LINESIZE 120
SELECT table_name, num_rows FROM (
  SELECT 'MA_MAESTRO_EQUIPOS' table_name, COUNT(*) num_rows FROM MAQUINARIAN.MA_MAESTRO_EQUIPOS UNION ALL
  SELECT 'MA_CONFIG_DISPONIBILIDAD', COUNT(*) FROM MAQUINARIAN.MA_CONFIG_DISPONIBILIDAD UNION ALL
  SELECT 'MA_EQUIPOS_REQ_X_FAMILIA', COUNT(*) FROM MAQUINARIAN.MA_EQUIPOS_REQ_X_FAMILIA UNION ALL
  SELECT 'MA_DEQUIPOS_REQ_X_FAMILIA', COUNT(*) FROM MAQUINARIAN.MA_DEQUIPOS_REQ_X_FAMILIA UNION ALL
  SELECT 'MA_ENC_CONFIG_AVANCE', COUNT(*) FROM MAQUINARIAN.MA_ENC_CONFIG_AVANCE UNION ALL
  SELECT 'MA_OT_CONFIG_AVANCE', COUNT(*) FROM MAQUINARIAN.MA_OT_CONFIG_AVANCE);
EXIT
SQL
