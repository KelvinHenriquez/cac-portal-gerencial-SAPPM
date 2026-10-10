# Despliegue en el servidor (Etapa 9)

## Qué se instala

```
usuarios (navegador) ──443──> nginx ──> web  (Next.js)
                                  └──/api──> api  (.NET 8) ──1521──> Oracle de CAC (esquema del portal + vistas sobre MAQUINARIAN)
                                                        └──33xx──> SAP (RFC, Etapa 8b)
```

- Tres contenedores (`docker-compose.prod.yml`): **nginx** (HTTPS), **web** y **api**. El portal
  **no tiene base de datos propia**: todo lo que guarda vive en el Oracle de CAC (tablas `_PGPM`).
  Los contenedores no guardan estado: se pueden borrar y recrear sin perder nada.
- El job diario de extracción y recálculo corre dentro de la API (05:00 y al arrancar si hoy no hubo
  corrida). Se apaga con `JOB_HABILITADO=false`.
- Probado en la laptop con las mismas imágenes (ver "Prueba local" abajo).

## 1. El servidor: el mismo de Pago en Efectivo (03/10/2026)

Docker Desktop 4.93 (Engine 29.8.1, Compose 5.5.1) sobre WSL2, con **contenedores Linux**: es la
opción A de abajo y corre las mismas imágenes que se probaron. Ya tiene montado `pagoscac-prod`
(postgres, nginx en **443**, api, web). El portal convive así:

- **Proyecto aparte** (`portalgerencial-prod`): su propia red y sus contenedores; no toca nada de
  Pago en Efectivo.
- **Puertos:** el 80/443 son de Pago en Efectivo. El portal publica **8443** (HTTPS) y **8081**
  (redirige a 8443): `https://<servidor>:8443`. Se cambian con `PUERTO_HTTPS` / `PUERTO_HTTP`. Abrir
  el 8443 en el firewall de Windows.
- **Más adelante** (opcional), cuando haya nombres DNS para los dos sistemas, un solo nginx en el
  443 puede atender a ambos por nombre (`pagos...` y `portal-gerencial...`) y el portal deja de
  necesitar el puerto en la URL.
- **Recursos:** la VM de Docker tiene 4 CPU y 3,7 GB; Pago en Efectivo usa ~350 MB y el portal
  necesita ~500 MB más. Alcanza.
- **Dos cuidados con Docker Desktop en un servidor:**
  1. **Licencia:** la captura muestra el plan **Personal**. Docker Desktop exige suscripción paga
     (Pro/Team/Business) en empresas de más de 250 empleados o más de USD 10 millones de ingresos.
     Conviene confirmarlo con TI. Ya aplica hoy por Pago en Efectivo, no es nuevo por el portal.
  2. **Arranque:** Docker Desktop arranca cuando alguien inicia sesión en Windows. Si el servidor se
     reinicia y nadie entra, los contenedores no suben. Se resuelve con "Start Docker Desktop when you
     sign in" más inicio de sesión automático del usuario de servicio, o pasando a Docker Engine
     dentro de WSL2 como servicio.

## 1b. Opciones de Docker en Windows (referencia)

Las imágenes del portal son **Linux** (como en la laptop). En un servidor Windows hay dos caminos:

| Opción | Cómo | Notas |
|---|---|---|
| **A (recomendada)** | Windows Server 2022 + **WSL2** con Ubuntu y **Docker Engine** dentro de Ubuntu | Mismas imágenes que se probaron. Docker Engine es gratis. (Docker Desktop también sirve, pero en una empresa del tamaño de CAC requiere licencia Docker Business.) |
| B | Docker nativo de Windows Server (Mirantis/Moby) | Solo corre contenedores **Windows**: habría que rehacer las tres imágenes sobre Windows (nanoserver). Más pesado y menos probado. |

**SAP .NET Connector (NCo):** si la versión de NCo para .NET 8 que se consiga solo corre en
Windows, la extracción de SAP no puede ir en un contenedor Linux. En ese caso: la API en Docker con
`JOB_HABILITADO=false`, y el mismo programa publicado como **servicio de Windows** con el job
encendido y `DataSourceMode=Production` (el código ya separa el job por configuración; no hay que
reescribir nada). Se define al empezar la Etapa 8b, con NCo en la mano.

## 2. Lo que hace el DBA en Oracle (una vez)

Dos scripts en `docs/oracle`, en este orden (SQL Developer, abrir y F5). Los demás `.sql` de esa
carpeta son de exploración o del Sprint 0, o los llaman estos dos: no hay que correrlos aparte.

1. [oracle/1_DBA_usuario_y_permisos.sql](oracle/1_DBA_usuario_y_permisos.sql), **como DBA**: crea el
   usuario técnico del portal (pregunta nombre, sugerido `PGPM`; contraseña y tablespace), le da
   `SELECT` directo sobre las 15 tablas de MAQUINARIAN y `PARGER.PG_PERIODO`, y crea el rol
   **`ROL_PORTAL_GERENCIAL`**. Al final lista los 15 permisos.
2. [oracle/2_PORTAL_tablas_y_vistas.sql](oracle/2_PORTAL_tablas_y_vistas.sql), **conectado como el
   usuario técnico**: corre `pgpm_tablas.sql` (14 tablas, idempotente) y `vistas_produccion.sql`
   (11 vistas, empresa 30), y verifica: 0 objetos inválidos y cuántas filas ve cada vista.
   Tiene que estar en la misma carpeta que esos dos archivos.
3. [oracle/3_DBA_roles_portal.sql](oracle/3_DBA_roles_portal.sql), **como DBA**: crea los roles del
   portal y da `ROL_PORTAL_ADMIN` al usuario de TI.
**Quién entra y qué puede hacer** (roles de Oracle; se suman; hace falta al menos uno):

| Rol | Ve | Puede |
| --- | --- | --- |
| `ROL_PORTAL_CDS` | Centro de Servicios: Disponibilidad, Avance, Permanencia CDS | Solo consultar |
| `ROL_PORTAL_IND` | Industrial: Permanencia IND | Solo consultar |
| `ROL_PORTAL_GERENCIAL` | Las dos áreas | Solo consultar |
| `ROL_PORTAL_PARAMETROS` | Lo de sus otros roles | Cambiar parámetros de negocio |
| `ROL_PORTAL_ADMIN` | Todo | Todo: parámetros técnicos, Actualizar ahora y desde |

Estado de actualización y Parámetros los ven todos en modo consulta. La API aplica estos permisos
en cada consulta, no solo esconde botones. Se dan con `GRANT <rol> TO <usuario>;` y valen desde el
próximo inicio de sesión. Oracle le da cada rol al DBA que lo crea: quitárselo si no debe entrar.

Con eso, en `.env.prod`: `ORACLE_USUARIO_TECNICO`/`ORACLE_PASSWORD_TECNICO` del paso 1 y
`APLICAR_MIGRACIONES=false`.

Probados el 08/10/2026 contra la réplica local de MAQUINARIAN: mismo esquema que desarrollo.

## 3. Red y nombre

- Servidor → Oracle: puerto del listener (1521).
- Servidor → SAP: HTTPS a los servicios web (en DEV, puerto 44300).
- Usuarios → servidor: 8443 (y 8081, que solo redirige); 443/80 los usa Pago en Efectivo.
- Nombre DNS interno (p. ej. `portal-gerencial.cac.com.do`) y **certificado** emitido para ese
  nombre (CA interna de CAC o comercial) en `deploy/certs/fullchain.pem` y `deploy/certs/privkey.pem`.
- Correo (opcional): servidor SMTP para la alerta de datos desactualizados.

## 4. Instalar

En el servidor (dentro de Ubuntu/WSL2 si es la opción A):

```bash
git clone <repositorio> /opt/portal-gerencial && cd /opt/portal-gerencial
cp .env.prod.example .env.prod      # completar TODOS los valores (Oracle, SAP, puertos...); chmod 600 .env.prod
# copiar el certificado a deploy/certs/
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --build
```

Mientras el RFC no esté listo: `DATA_SOURCE_MODE=Local` y `JOB_HABILITADO=false` para mostrar las
pantallas sin datos de SAP, o esperar a la Etapa 8b.

## 5. Verificar

1. `docker compose -f docker-compose.prod.yml ps`: los tres en `running`, la api `healthy`.
2. `https://<servidor>:8443/api/salud` → `Healthy` (la API llega a Oracle con el usuario técnico).
3. Entrar con un usuario nombrado de Oracle.
4. Sistema → Estado de actualización: última corrida OK y próxima a las 05:00.

## 6. Operación

| Tarea | Cómo |
|---|---|
| Ver logs | `docker compose -f docker-compose.prod.yml logs -f api` |
| Actualizar versión | `git pull` y el mismo `up -d --build`; las migraciones nuevas se aplican solas (o el DBA corre el `pgpm_tablas.sql` nuevo) |
| Volver a la versión anterior | `git checkout <commit anterior>` y `up -d --build`. Las migraciones solo agregan; no hace falta tocar Oracle |
| Correr la actualización a mano | botón "Actualizar ahora" en Sistema → Estado de actualización |
| Cambiar una regla de cálculo | Sistema → Parámetros (`ROL_PORTAL_PARAMETROS`; los técnicos, `ROL_PORTAL_ADMIN`; queda quién y cuándo) |
| Respaldo | nada en el servidor: el respaldo es el de Oracle |
| Reinicio del servidor | los contenedores tienen `restart: unless-stopped`: suben solos con Docker |

## Prueba local del despliegue (laptop)

Mismas imágenes de producción, nginx con HTTPS y certificado autofirmado, contra el Oracle de
desarrollo y SAP simulado:

```bash
bash scripts/certificado_local.sh
docker compose --env-file deploy/prod-local.env -f docker-compose.prod.yml -f docker-compose.prod-local.yml up -d --build
# https://localhost:8443  (8081 redirige a 8443, como en el servidor)
docker compose --env-file deploy/prod-local.env -f docker-compose.prod.yml -f docker-compose.prod-local.yml down
```
