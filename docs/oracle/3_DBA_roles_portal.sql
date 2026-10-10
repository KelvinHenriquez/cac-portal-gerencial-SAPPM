-- =====================================================================================
-- PRODUCCIÓN — Roles del portal (10/10/2026). Lo corre un DBA, una vez, ANTES de actualizar
-- el portal a la versión con roles. SQL Developer: abrir y F5.
--
-- Cada rol es solo una "llave": no da permisos sobre tablas (los reportes se leen con el
-- usuario técnico PGPM). Para entrar al portal hace falta al menos uno; se suman entre sí.
--
--   ROL_PORTAL_CDS         Centro de Servicios: Disponibilidad, Avance y Permanencia de CDS. Solo ver.
--   ROL_PORTAL_IND         Industrial: Permanencia de IND (y los reportes industriales que vengan). Solo ver.
--   ROL_PORTAL_GERENCIAL   Gerencia: las dos áreas. Solo ver. (Ya existía: quien lo tiene sigue igual.)
--   ROL_PORTAL_PARAMETROS  Además puede cambiar los parámetros de negocio. Va junto a un rol de área.
--   ROL_PORTAL_ADMIN       TI: todo, incluidos los parámetros técnicos y "Actualizar ahora / desde".
--
-- Pregunta un valor: USUARIO_ADMIN, el usuario de Oracle de TI que será administrador del
-- portal (sugerido: KHENRIQUEZ). Antes, ese permiso venía de ADMIN_USUARIOS en el servidor.
-- =====================================================================================
SET DEFINE ON
SET VERIFY OFF
ACCEPT USUARIO_ADMIN CHAR DEFAULT 'KHENRIQUEZ' PROMPT 'Usuario de TI administrador del portal [KHENRIQUEZ]: '

PROMPT ===== 1. Roles (si alguno ya existe da ORA-01921 y sigue) =====
WHENEVER SQLERROR CONTINUE
CREATE ROLE ROL_PORTAL_ADMIN;
CREATE ROLE ROL_PORTAL_PARAMETROS;
CREATE ROLE ROL_PORTAL_GERENCIAL;
CREATE ROLE ROL_PORTAL_CDS;
CREATE ROLE ROL_PORTAL_IND;
WHENEVER SQLERROR EXIT FAILURE

PROMPT ===== 2. Administrador del portal =====
GRANT ROL_PORTAL_ADMIN TO &&USUARIO_ADMIN;

PROMPT ===== 3. Quién tiene cada rol =====
SELECT GRANTED_ROLE AS ROL, GRANTEE AS USUARIO
FROM   DBA_ROLE_PRIVS
WHERE  GRANTED_ROLE LIKE 'ROL\_PORTAL\_%' ESCAPE '\'
ORDER BY 1, 2;

PROMPT Listo. Oracle le da cada rol también al DBA que lo creó: quíteselo si no debe entrar al portal.

-- =====================================================================================
-- DAR Y QUITAR ACCESO (cuando haga falta, como DBA):
--
--   GRANT  ROL_PORTAL_CDS TO <USUARIO>;                           -- Centro de Servicios
--   GRANT  ROL_PORTAL_CDS, ROL_PORTAL_PARAMETROS TO <USUARIO>;    -- CDS que además ajusta reglas
--   GRANT  ROL_PORTAL_IND TO <USUARIO>;                           -- Industrial
--   GRANT  ROL_PORTAL_GERENCIAL TO <USUARIO>;                     -- Gerencia (las dos áreas)
--   REVOKE ROL_PORTAL_CDS FROM <USUARIO>;                         -- quitar
--
-- El cambio vale desde el próximo inicio de sesión de esa persona en el portal.
-- =====================================================================================
