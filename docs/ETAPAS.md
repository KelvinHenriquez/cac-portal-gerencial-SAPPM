# Plan por etapas — Portal Gerencial SAP PM

Especificación original: [Portal_Talleres_Disponibilidad_Avance_Especificacion.docx](Portal_Talleres_Disponibilidad_Avance_Especificacion.docx)
(borrador del 30/09/2026). Las secciones citadas en el código ("sección 4.3", etc.) son de ese documento.

El portal nace con los dos reportes del Centro de Servicios (Taller), pero es el lugar donde se irán
montando todos los reportes y tableros gerenciales, incluida el área industrial en el futuro.

| # | Etapa | Estado |
|---|---|---|
| 1 | Esqueleto: copia de Pago en Efectivo sin su dominio, Oracle Free local, login contra Oracle, shell del portal | **Hecha** |
| 2 | Modelo de datos `_pgpm` + estructura de Oracle Forms simulada (familias, curva, requeridos, umbrales, equipos) | **Hecha** |
| 3 | SAP simulado + Background Service de extracción y recálculo + bitácora + equipos sin clasificar | **Hecha** |
| 4 | Pantalla Disponibilidad de Maquinaria | Pendiente |
| 5 | Pantalla Avance de Reparación | Pendiente |
| 6 | Frescura de datos, monitoreo, alerta > 24 h, pruebas del cálculo | Pendiente |
| 7 | Integración real: DDL en Oracle de CAC, vistas reales, cliente RFC con NCo | Bloqueada por pendientes externos |
| 0 | Sprint 0: validar el cálculo con exports reales de SAP ([lista de exports](SPRINT0_EXPORTS_SAP.md)) | Esperando los exports |
| 8 | Despliegue en el servidor Docker | Después de la 7 |

## Decisiones tomadas (30/09/2026)

- **Nombre y alcance:** `C:\cac-portal-gerencial-SAPPM`. El menú y la portada se organizan por área
  de negocio (hoy: Centro de Servicios; a futuro: Área Industrial).
- **Base del código:** copiado de `C:\cac-pagos-efectivo` (que ya traía el UI/UX y la arquitectura
  de TPresto sin el dominio de préstamos), y luego se quitó el dominio de pagos.
- **Base de datos local: Oracle Free en Docker, no Postgres.** Producción es Oracle, así que se usa
  el mismo motor, el mismo provider de EF y el mismo SQL en ambos ambientes. Además permite probar el
  login real (sección 4.3) con usuarios nombrados locales. No requiere instalar nada fuera de Docker.
- **DataSourceMode** ahora solo decide de dónde salen los datos de SAP (simulado o RFC real). La
  estructura de Oracle Forms se lee siempre por las vistas `_pgpm`; en local apuntan al esquema
  simulado `SISTEMA_OT`.
- **Esquema propio:** `PGPM` en local (la spec usaba `taller` como placeholder). El nombre real lo
  define el DBA; se configura en `Oracle:Esquema` sin tocar código.
- **Disponibilidad topada en 100 %**, por estética del reporte.
- **De diario a semanal:** se completa a nivel de tabla (Etapa 2) con una tabla de resultados
  semanales, para que no haya hueco entre lo que se calcula (por día) y lo que se muestra (por semana).
- **Límite de intentos de login:** 5 fallos en 15 minutos frenan al usuario 15 minutos en el portal
  (Oracle bloquea a los 10 en el perfil DEFAULT, así que el portal frena antes y no le bloquea la
  cuenta). Además, 20 intentos por minuto por IP.
- **Permisos del usuario nombrado sobre las vistas:** deja de ser pendiente. El usuario nombrado solo
  se usa para validar el login; todas las consultas las hace el usuario técnico del portal.

## Modelo de datos — diferencias con la sección 3 de la spec (Etapa 2)

Script para el DBA (idempotente, generado de las migraciones): [oracle/pgpm_tablas.sql](oracle/pgpm_tablas.sql).
Vistas de producción con los huecos a llenar: [oracle/vistas_produccion.sql](oracle/vistas_produccion.sql).

- `calc_disponibilidad_diaria_pgpm` se llama **`CALC_DISP_DIARIA_PGPM`**: el nombre original tiene
  31 caracteres y Oracle anterior a 12.2 admite 30. Todos los nombres del portal caben en 30.
- La disponibilidad diaria lleva **`TIPO_REPORTE` en la llave**: los requeridos dependen del tipo de
  reporte, y una familia puede estar en dos tipos (Tractores en CS-P30 y en Nivelación).
- Tablas nuevas para que no quede hueco:
  - `CALC_DISP_SEMANAL_PGPM` — el agregado semanal que pinta el semáforo (promedio de la semana y
    peor día; semana ISO lunes-domingo). El color se calcula al leer, contra los umbrales vigentes.
  - `STG_SAP_HIST_ESTADO_PGPM` — destino del módulo `Z_PM_TALLERES_HISTORIAL_ESTADO` (JCDS), que la
    spec describe en el Anexo A pero no tenía tabla.
  - `CALC_EQUIPO_SIN_CLASIF_PGPM` — los "equipos sin clasificar" de la sección 10.
  - `PARAMETRO_PGPM` — los supuestos pendientes (estados fuera de servicio, estados cerrados, clase
    ZC03, hora del job, horas de alerta) quedan como parámetros, no en el código.
- `CALC_AVANCE_SEMANAL_PGPM` guarda también los insumos (órdenes totales/cerradas, horas plan/real)
  para auditar el % y poder cambiar la fórmula de "Total Ejec. %" sin perder trazabilidad.
- `STG_SAP_ORDEN_PGPM` suma `DESCRIPCION` (texto breve, para el detalle de órdenes del día) y
  `FECHA_CREACION`. `LOG_EXTRACCION_PGPM` suma `CORRIDA_ID`, `FECHA_CORTE_DESDE` y `ORIGEN`.
- Vistas: `VW_CURVA_PLANEADA_PGPM` lleva `FAMILIA_CODIGO` (la curva es por familia) y hay una
  quinta vista, `VW_EQUIPOS_FAMILIA_PGPM` (equipo → familia por código legado).

## Job de actualización y reglas de cálculo (Etapa 3)

- **Cuándo corre:** todos los días a la hora de `JOB.HORA_CORRIDA_DIARIA` (05:00), y además al
  arrancar la API si hoy todavía no hubo una corrida exitosa. Se puede correr a mano con
  `POST /api/sistema/corridas` (luego con un botón en la pantalla de Estado de actualización). Nunca
  corren dos a la vez. `Job:Habilitado=false` lo apaga.
- **Pasos:** equipos → órdenes → historial de estados → horas (cada uno con delta desde la última
  extracción OK de esa fuente, con un día de solape) → equipos sin clasificar → disponibilidad
  (últimos `JOB.DIAS_RECALCULO_DISPONIBILIDAD` días) → avance. Cada paso queda en
  `LOG_EXTRACCION_PGPM`. Si uno falla, la corrida se detiene y los resultados anteriores quedan
  intactos; el encabezado lo avisa (probado rompiendo una vista a propósito).
- **Ajuste a la regla de la sección 6.1:** una orden con fecha fin real cuenta como indisponibilidad
  entre inicio y fin real **sin importar su estado actual**. El estado (Liberado / En Proceso) solo
  se usa para las órdenes que todavía no tienen fecha fin: cuentan hasta hoy. Si se mirara solo el
  estado actual, cerrar técnicamente una orden borraría su indisponibilidad del pasado.
- **% real de avance (provisional mientras se confirma "Total Ejec. %"):** horas planificadas de las
  órdenes ZC03 cerradas técnicamente / horas planificadas de todas las ZC03 de la familia en el
  programa (`AVANCE.FORMULA_REAL=HH_ORDENES_CERRADAS`). Alternativa ya programada:
  `ORDENES_CERRADAS` (cantidad). Una orden pertenece al programa si su inicio planificado cae en
  la ventana de la familia (con 30 días de margen previo).
- **SAP simulado:** determinista (mismo día, mismos datos) y las órdenes avanzan solas con los
  días. Incluye brechas reales: ~5 % de reparaciones terminadas sin cierre técnico y ~15 % de
  órdenes sin horas confirmadas, más 3 equipos que solo existen en SAP.

## Datos reales de SAP

Para desarrollar (etapas 3 a 6) **no hacen falta exports de SAP**: el cliente SAP simulado genera
órdenes, horas e historial coherentes con los equipos de la estructura local. Los datos reales
entran en dos momentos:

1. **Sprint 0 (validación temprana, sección 6.3):** un export de SAP de 2-3 familias en un período
   ya cerrado, para comparar la disponibilidad derivada contra lo que taller sabe que pasó. Se
   puede hacer ya, sin esperar el RFC: lista exacta de transacciones y columnas en
   [SPRINT0_EXPORTS_SAP.md](SPRINT0_EXPORTS_SAP.md).
2. **Producción (Etapa 7):** los datos llegan solos por RFC; no hay Excel en el flujo normal.

## Pendientes

Responsable: **Kelvin (DBA)**
- [ ] Esquema/tablespace real para los objetos `_pgpm` y usuario técnico del portal con CREATE TABLE,
      CREATE VIEW, CREATE SEQUENCE (o correr el script de migraciones una vez y dejarle solo DML).
- [ ] Nombres reales de las tablas del Sistema de Órdenes de Trabajo: familias, curva por semana,
      equipos requeridos, umbrales — **y la tabla de equipos (Objeto Técnico / Ficha) con su familia**,
      que la spec no lista pero hace falta para el cruce por código legado y el detalle por familia.
- [ ] Versión del Oracle de producción (`SELECT banner FROM v$version`) para fijar `Oracle:VersionSql`.
- [x] En la curva planeada, `ACTIVIDAD_PCT` es el % de cada semana (no acumulado); el portal lo
      acumula. Confirmado el 30/09/2026.
- [ ] Fórmulas de "Disponibilidad Comp. %" y "Total Ejec. %" (idealmente el PL/SQL original).
- [ ] Set exacto de estados SAP que cuentan como "fuera de servicio" (hoy: `Liberado,En Proceso`
      en `PARAMETRO_PGPM`).
- [ ] Fecha de arranque de SAP PM (para elegir el período del Sprint 0 y `SAP.FECHA_CARGA_INICIAL`).
- [ ] Criterio numérico de aceptación del Sprint 0.

Responsable: **ABAP** (en curso, ~2 días desde el 30/09)
- [ ] Módulos RFC del Anexo A, campo real del código legado, retención de JCDS.

Responsable: **Basis / Infraestructura**
- [ ] Marcar los 4 módulos como Remote-Enabled, usuario de comunicación con S_RFC, firewall al gateway.
- [ ] Licencia/instalación de SAP .NET Connector (NCo). Ojo: Docker en Windows corre contenedores
      **Linux** por defecto (como en la laptop). Hay que confirmar que NCo corre dentro de un
      contenedor Linux; si no, el job de extracción puede correr como servicio de Windows fuera de
      Docker, o en un contenedor Windows. Se valida al inicio de la Etapa 7.
