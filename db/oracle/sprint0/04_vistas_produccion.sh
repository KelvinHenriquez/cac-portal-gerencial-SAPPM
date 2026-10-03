#!/bin/bash
# SOLO SPRINT 0. Crea las vistas _PGPM con el MISMO script que se correrá en producción
# (docs/oracle/vistas_produccion.sql), sobre la réplica de MAQUINARIAN.
sqlplus -s / as sysdba <<SQL
ALTER SESSION SET CURRENT_SCHEMA = PGPM;
DEFINE ID_EMPRESA = ${SPRINT0_ID_EMPRESA:-1}
@/opt/pgpm/docs/vistas_produccion.sql
SELECT object_name, status FROM dba_objects WHERE owner = 'PGPM' AND object_type = 'VIEW' ORDER BY 1;
EXIT
SQL
