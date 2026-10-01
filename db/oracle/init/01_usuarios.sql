-- =====================================================================================
-- Oracle Free local (solo desarrollo). Corre UNA vez, la primera vez que se crea el
-- volumen del contenedor portalgerencial-oracle-dev, como SYSDBA.
--
-- Simula los tres tipos de usuario que habrá en el Oracle real de CAC:
--   PGPM        -> usuario técnico del portal, dueño de los objetos _pgpm (sección 3).
--                  En producción el nombre real lo define el DBA (placeholder de la spec: "taller").
--   SISTEMA_OT  -> simula el esquema del módulo "Sistema de Órdenes de Trabajo" de Oracle
--                  Forms (familias, curva, equipos requeridos, umbrales). Etapa 2.
--   GERENTE1..  -> usuarios nombrados con los que la gente entra hoy a Oracle Forms y con los
--                  que entrará al portal (sección 4.3). Solo necesitan CREATE SESSION.
-- =====================================================================================
ALTER SESSION SET CONTAINER = FREEPDB1;

-- Perfil parecido al DEFAULT de Oracle: 10 intentos fallidos bloquean la cuenta. El portal
-- frena a los 5 (Login:MaxIntentosFallidos) para no llegar nunca a este bloqueo.
CREATE PROFILE PERFIL_USUARIOS_NOMBRADOS LIMIT
  FAILED_LOGIN_ATTEMPTS 10
  PASSWORD_LOCK_TIME 1/24;

-- Usuario técnico del portal
CREATE USER PGPM IDENTIFIED BY "PgpmLocal2026"
  DEFAULT TABLESPACE USERS QUOTA UNLIMITED ON USERS;
GRANT CREATE SESSION, CREATE TABLE, CREATE VIEW, CREATE SEQUENCE TO PGPM;

-- Esquema simulado del Sistema de Órdenes de Trabajo (Oracle Forms)
CREATE USER SISTEMA_OT IDENTIFIED BY "SistemaOtLocal2026"
  DEFAULT TABLESPACE USERS QUOTA UNLIMITED ON USERS;
GRANT CREATE SESSION, CREATE TABLE TO SISTEMA_OT;

-- Usuarios nombrados de prueba
CREATE USER GERENTE1 IDENTIFIED BY "Gerente2026" PROFILE PERFIL_USUARIOS_NOMBRADOS;
GRANT CREATE SESSION TO GERENTE1;

CREATE USER GERENTE2 IDENTIFIED BY "Gerente2026" PROFILE PERFIL_USUARIOS_NOMBRADOS;
GRANT CREATE SESSION TO GERENTE2;

-- Para probar el mensaje "Su usuario está bloqueado en Oracle"
CREATE USER BLOQUEADO1 IDENTIFIED BY "Gerente2026" PROFILE PERFIL_USUARIOS_NOMBRADOS ACCOUNT LOCK;
GRANT CREATE SESSION TO BLOQUEADO1;
