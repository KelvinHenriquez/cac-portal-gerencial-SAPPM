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

La primera vez descarga la imagen de Oracle Free (~1.5 GB) y crea los usuarios de
[db/oracle/init/01_usuarios.sql](db/oracle/init/01_usuarios.sql).

| Servicio | URL / conexión |
|---|---|
| Frontend | http://localhost:3002 |
| API + Swagger | http://localhost:5402/swagger |
| Oracle Free | `localhost:1522/FREEPDB1` (SQL Developer, DBeaver, etc.) |

Los puertos no chocan con TPresto (3000/5400/5432) ni con Pago en Efectivo (3001/5401/5433).

### Usuarios de prueba (Oracle local)

El login valida contra Oracle, igual que en producción (sección 4.3): no hay tabla de usuarios propia.

| Usuario | Clave | Para qué |
|---|---|---|
| `gerente1`, `gerente2` | `Gerente2026` | Entrar al portal |
| `bloqueado1` | `Gerente2026` | Ver el mensaje de cuenta bloqueada en Oracle |
| `PGPM` | `PgpmLocal2026` | Usuario técnico del portal (dueño de las tablas `_pgpm`) |
| `SYSTEM` | `OracleLocal2026` | Administración de la base local |

Para empezar de cero la base local (borra todo y vuelve a correr los scripts de init):

```bash
docker compose -f docker-compose.dev.yml down -v
```
