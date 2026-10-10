# Portal Gerencial SAP PM — CAC

Portal web de solo consulta para los reportes y tableros gerenciales del Consorcio Azucarero
Central (CAC), construidos sobre los datos operativos de SAP PM cruzados con la estructura de
negocio que vive en Oracle. Arranca con los dos reportes del Centro de Servicios (Taller) —
Disponibilidad de Maquinaria y Avance de Reparación — y está pensado para sumar después los del
área industrial.

- Especificación: [docs/Portal_Talleres_Disponibilidad_Avance_Especificacion.docx](docs/Portal_Talleres_Disponibilidad_Avance_Especificacion.docx)
- Plan por etapas, decisiones y pendientes: [docs/ETAPAS.md](docs/ETAPAS.md)

Base de código copiada de [Pago en Efectivo](../cac-pagos-efectivo) (que a su vez porta la
arquitectura y el UI/UX de TPresto): .NET 8 Clean Architecture (Core / Infrastructure / API),
Next.js 15 / React 19 / MUI v6, Docker Compose.

## Levantar en local

Requisito: Docker Desktop. Nada más — Oracle corre en un contenedor.

```bash
docker compose -f docker-compose.dev.yml up -d
```

La primera vez descarga la imagen de Oracle 11g XE (la misma generación que el Oracle de producción, 11.2.0.4) y corre los scripts de
[db/oracle/init/](db/oracle/init/): usuarios, el esquema `SISTEMA_OT` que simula las tablas de
configuración de Oracle Forms (familias, curva planeada, equipos requeridos, umbrales y equipos del
Programa de Reparación 2026) y las vistas `VW_*_PGPM` encima. Las tablas `_pgpm` del portal las
crea la API al arrancar (migraciones de EF).

Los scripts de init solo corren cuando el volumen de Oracle es nuevo. Si se agrega o cambia uno,
o se reinicia la base (`down -v`, abajo), o se corre a mano:

```bash
docker exec portalgerencial-oracle-dev bash -c "echo exit | sqlplus -s / as sysdba @/container-entrypoint-initdb.d/02_sistema_ot.sql"
```

| Servicio | URL / conexión |
|---|---|
| Frontend | http://localhost:3002 |
| API + Swagger | http://localhost:5402/swagger |
| Oracle Free | `localhost:1522/XE` (SQL Developer, DBeaver, etc.) |
| Mailpit (correos de alerta de prueba) | http://localhost:8025 |

Los puertos no chocan con TPresto (3000/5400/5432) ni con Pago en Efectivo (3001/5401/5433).

### Usuarios de prueba (Oracle local)

El login valida contra Oracle, igual que en producción (sección 4.3): no hay tabla de usuarios propia.

| Usuario | Clave | Para qué |
|---|---|---|
| `gerente1` | `Gerente2026` | Administrador (TI): todo |
| `gerente2` | `Gerente2026` | Gerencia: las dos áreas, solo ver |
| `cds1` | `Gerente2026` | Centro de Servicios, solo ver |
| `ind1` | `Gerente2026` | Industrial, solo ver |
| `param1` | `Gerente2026` | Centro de Servicios + cambia parámetros de negocio |
| `bloqueado1` | `Gerente2026` | Ver el mensaje de cuenta bloqueada en Oracle |
| `sinacceso1` | `Gerente2026` | Ver el mensaje de usuario sin ningún rol del portal |
| `PGPM` | `PgpmLocal2026` | Usuario técnico del portal (dueño de las tablas `_pgpm`) |
| `SYSTEM` | `OracleLocal2026` | Administración de la base local |

Para empezar de cero la base local (borra todo y vuelve a correr los scripts de init):

```bash
docker compose -f docker-compose.dev.yml down -v
```
