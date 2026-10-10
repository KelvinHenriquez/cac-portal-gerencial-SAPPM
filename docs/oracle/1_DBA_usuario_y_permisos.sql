-- =====================================================================================
-- PRODUCCIÓN, PASO 1 de 2 — Lo corre un usuario DBA (SYSTEM o equivalente), una sola vez.
-- SQL Developer: abrir este archivo y ejecutar como script (F5).
--
-- Qué hace:
--   1. Crea el usuario técnico del portal (dueño de las tablas y vistas *_PGPM).
--   2. Le da permiso de SOLO LECTURA (SELECT) sobre las 15 tablas de Oracle Forms que leen las
--      vistas del portal (MAQUINARIAN) y sobre PARGER.PG_PERIODO.
--   3. Los roles del portal (ROL_PORTAL_*) se crean con 3_DBA_roles_portal.sql, después de este.
--   No crea tablas, no toca datos de MAQUINARIAN ni de PARGER.
--
-- Al correrlo, SQL Developer pregunta tres valores:
--   USUARIO_PORTAL  nombre del usuario técnico (sugerido: PGPM)
--   CLAVE_PORTAL    su contraseña (no se guarda en este archivo; anótela solo en el .env.prod
--                   del servidor, ORACLE_PASSWORD_TECNICO)
--   TABLESPACE      donde guardará sus tablas (por ejemplo USERS; los cálculos ocupan poco)
--
-- Si el usuario ya existe, el CREATE USER da ORA-01920 y el script sigue con los permisos.
-- Después: correr 2_PORTAL_tablas_y_vistas.sql conectado como ese usuario.
-- =====================================================================================
SET DEFINE ON
SET VERIFY OFF
ACCEPT USUARIO_PORTAL CHAR DEFAULT 'PGPM' PROMPT 'Usuario técnico del portal [PGPM]: '
ACCEPT CLAVE_PORTAL   CHAR PROMPT 'Contraseña del usuario técnico: ' HIDE
ACCEPT TABLESPACE     CHAR DEFAULT 'USERS' PROMPT 'Tablespace [USERS]: '

PROMPT ===== 1. Usuario técnico =====
WHENEVER SQLERROR CONTINUE
CREATE USER &&USUARIO_PORTAL IDENTIFIED BY "&&CLAVE_PORTAL"
  DEFAULT TABLESPACE &&TABLESPACE QUOTA UNLIMITED ON &&TABLESPACE;
GRANT CREATE SESSION, CREATE TABLE, CREATE VIEW, CREATE SEQUENCE TO &&USUARIO_PORTAL;

PROMPT ===== 2. Lectura de Oracle Forms (directo, no por rol: las vistas lo necesitan así) =====
WHENEVER SQLERROR EXIT FAILURE
GRANT SELECT ON MAQUINARIAN.MA_MAESTRO_EQUIPOS        TO &&USUARIO_PORTAL;
GRANT SELECT ON MAQUINARIAN.MA_MST_FAMILIA            TO &&USUARIO_PORTAL;
GRANT SELECT ON MAQUINARIAN.MA_TIPO_ORDEN_TRABAJO     TO &&USUARIO_PORTAL;
GRANT SELECT ON MAQUINARIAN.MA_CONFIG_DISPONIBILIDAD  TO &&USUARIO_PORTAL;
GRANT SELECT ON MAQUINARIAN.MA_CONFIG_SEMANA_DISP     TO &&USUARIO_PORTAL;
GRANT SELECT ON MAQUINARIAN.MA_CONFIG_HORA_DISP       TO &&USUARIO_PORTAL;
GRANT SELECT ON MAQUINARIAN.MA_EQUIPOS_REQ_X_FAMILIA  TO &&USUARIO_PORTAL;
GRANT SELECT ON MAQUINARIAN.MA_DEQUIPOS_REQ_X_FAMILIA TO &&USUARIO_PORTAL;
GRANT SELECT ON MAQUINARIAN.MA_ENC_CONFIG_AVANCE      TO &&USUARIO_PORTAL;
GRANT SELECT ON MAQUINARIAN.MA_DET_CONFIG_AVANCE      TO &&USUARIO_PORTAL;
GRANT SELECT ON MAQUINARIAN.MA_SUBDET_CONFIG_AVANCE   TO &&USUARIO_PORTAL;
GRANT SELECT ON MAQUINARIAN.MA_OT_CONFIG_AVANCE       TO &&USUARIO_PORTAL;
GRANT SELECT ON MAQUINARIAN.MA_ENC_SEMANA_OT          TO &&USUARIO_PORTAL;
GRANT SELECT ON MAQUINARIAN.MA_DET_SEMANA_OT          TO &&USUARIO_PORTAL;
GRANT SELECT ON PARGER.PG_PERIODO                     TO &&USUARIO_PORTAL;

PROMPT ===== 3. Verificación: deben salir 15 filas =====
SELECT OWNER, TABLE_NAME, PRIVILEGE
FROM   DBA_TAB_PRIVS
WHERE  GRANTEE = UPPER('&&USUARIO_PORTAL')
ORDER BY OWNER, TABLE_NAME;

UNDEFINE CLAVE_PORTAL
PROMPT Listo. Siguiente: conectarse como &&USUARIO_PORTAL y correr 2_PORTAL_tablas_y_vistas.sql

-- Siguiente, también como DBA: 3_DBA_roles_portal.sql (roles del portal y su administrador).
