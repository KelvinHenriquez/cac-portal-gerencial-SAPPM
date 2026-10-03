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

## 1. Decisión pendiente: qué Docker corre en el servidor Windows

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

1. Usuario técnico del portal (dueño de los objetos `_PGPM`), con tablespace y cuota:
   ```sql
   CREATE USER <PORTAL> IDENTIFIED BY "<clave>" DEFAULT TABLESPACE <TS> QUOTA UNLIMITED ON <TS>;
   GRANT CREATE SESSION, CREATE TABLE, CREATE VIEW, CREATE SEQUENCE, CREATE SYNONYM TO <PORTAL>;
   ```
2. Tablas del portal: correr [oracle/pgpm_tablas.sql](oracle/pgpm_tablas.sql) como `<PORTAL>`
   (idempotente). Alternativa: dejar `APLICAR_MIGRACIONES=true` y que la API las cree al arrancar.
   Si el DBA prefiere que el usuario técnico no tenga DDL, usar el script y `APLICAR_MIGRACIONES=false`.
3. Permisos de lectura sobre MAQUINARIAN (directos, no por rol) y sinónimo de períodos: la lista
   está al inicio de [oracle/vistas_produccion.sql](oracle/vistas_produccion.sql).
4. Vistas: correr `vistas_produccion.sql` como `<PORTAL>` (pide `ID_EMPRESA`: **30**).
5. Usuarios nombrados (los gerentes): no necesitan nada nuevo; entran al portal con su usuario y
   clave de Oracle. Solo hace falta `CREATE SESSION`, que ya tienen.

## 3. Red y nombre

- Servidor → Oracle: puerto del listener (1521).
- Servidor → SAP: gateway 33xx (Etapa 8b).
- Usuarios → servidor: 443 (y 80, que solo redirige a 443).
- Nombre DNS interno (p. ej. `portal-gerencial.cac.com.do`) y **certificado** emitido para ese
  nombre (CA interna de CAC o comercial) en `deploy/certs/fullchain.pem` y `deploy/certs/privkey.pem`.
- Correo (opcional): servidor SMTP para la alerta de datos desactualizados.

## 4. Instalar

En el servidor (dentro de Ubuntu/WSL2 si es la opción A):

```bash
git clone <repositorio> /opt/portal-gerencial && cd /opt/portal-gerencial
cp .env.prod.example .env.prod      # completar TODOS los valores; chmod 600 .env.prod
# copiar el certificado a deploy/certs/
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --build
```

Mientras el RFC no esté listo: `DATA_SOURCE_MODE=Local` y `JOB_HABILITADO=false` para mostrar las
pantallas sin datos de SAP, o esperar a la Etapa 8b.

## 5. Verificar

1. `docker compose -f docker-compose.prod.yml ps`: los tres en `running`, la api `healthy`.
2. `https://<servidor>/api/salud` → `Healthy` (la API llega a Oracle con el usuario técnico).
3. Entrar con un usuario nombrado de Oracle.
4. Sistema → Estado de actualización: última corrida OK y próxima a las 05:00.

## 6. Operación

| Tarea | Cómo |
|---|---|
| Ver logs | `docker compose -f docker-compose.prod.yml logs -f api` |
| Actualizar versión | `git pull` y el mismo `up -d --build`; las migraciones nuevas se aplican solas (o el DBA corre el `pgpm_tablas.sql` nuevo) |
| Volver a la versión anterior | `git checkout <commit anterior>` y `up -d --build`. Las migraciones solo agregan; no hace falta tocar Oracle |
| Correr la actualización a mano | botón "Actualizar ahora" en Sistema → Estado de actualización |
| Respaldo | nada en el servidor: el respaldo es el de Oracle |
| Reinicio del servidor | los contenedores tienen `restart: unless-stopped`: suben solos con Docker |

## Prueba local del despliegue (laptop)

Mismas imágenes de producción, nginx con HTTPS y certificado autofirmado, contra el Oracle de
desarrollo y SAP simulado:

```bash
bash scripts/certificado_local.sh
docker compose --env-file deploy/prod-local.env -f docker-compose.prod.yml -f docker-compose.prod-local.yml up -d --build
# https://localhost:8443
docker compose --env-file deploy/prod-local.env -f docker-compose.prod.yml -f docker-compose.prod-local.yml down
```
