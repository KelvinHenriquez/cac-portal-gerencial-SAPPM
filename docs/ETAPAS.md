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
| 4 | Pantalla Disponibilidad de Maquinaria | **Hecha** |
| 5 | Pantalla Avance de Reparación | **Hecha** |
| 5b | Alinear Avance con el reporte de Oracle (capturas del 30/09/2026) | Pendiente de respuestas |
| 6 | Frescura de datos, monitoreo, alerta > 24 h, pruebas del cálculo | **Hecha** |
| 7 | Integración real: DDL en Oracle de CAC, vistas reales, cliente RFC con NCo | Bloqueada por pendientes externos |
| 0 | Sprint 0: validar el cálculo con exports reales de SAP ([lista de exports](SPRINT0_EXPORTS_SAP.md)) | Esperando los exports |
| 8 | Despliegue en el servidor Docker | Después de la 7 |

## Decisiones tomadas (30/09/2026)

- **Nombre y alcance:** `C:\cac-portal-gerencial-SAPPM`. El menú y la portada se organizan por área
  de negocio (hoy: Centro de Servicios; a futuro: Área Industrial).
- **Base del código:** copiado de `C:\cac-pagos-efectivo` (que ya traía el UI/UX y la arquitectura
  de TPresto sin el dominio de préstamos), y luego se quitó el dominio de pagos.
- **Base de datos local: Oracle 11g XE en Docker** (al principio Oracle Free 23; se cambió el
  01/10/2026 al saber que producción es **Oracle 11.2.0.4**). Mismo motor y misma generación que
  producción: cualquier cosa que solo exista desde 12c falla en desarrollo. Permite además probar
  el login real (sección 4.3) con usuarios nombrados locales.
- **Reglas por ser Oracle 11g:** sin columnas IDENTITY ni `DEFAULT seq.NEXTVAL` (los Id son llaves
  naturales o GUID generados por la aplicación); sin `Take`/`First`/`Skip` en consultas a la base
  (EF los traduce a `FETCH FIRST`, que no existe en 11g: se recorta en memoria o con ROWNUM);
  nombres de objetos de 30 caracteres o menos. El driver ODP.NET 23 conecta a 11g (probado con XE
  11.2.0.2); el provider de EF se configura en compatibilidad 19 porque no tiene opción 11.
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

## Pantalla Disponibilidad de Maquinaria (Etapa 4)

- Selector de tipo de reporte (de `VW_UMBRALES_SEMAFORO_PGPM`) y de semanas (4, 8, 13 o 26);
  ambos se recuerdan en el navegador.
- Columnas actuales (Cant. Equipo, Eq. Requerido, Eq. Disponible, Disponibilidad %, Potencial %)
  = último día calculado. Columnas semanales = promedio de los días de la semana ISO, con el peor
  día y los días calculados en el tooltip. Semáforo coloreado al leer, con los umbrales vigentes.
- **Fila "Todas (Promedio)"** (decidido el 30/09/2026): total disponible / total requerido, "porque
  así es menos engañoso". De cada familia se cuentan como máximo sus requeridos, para que el
  sobrante de una (p. ej. 11 cosechadoras para 10) no tape la falta de otra. Potencial % = total
  disponible / total de equipos.
- Drill-down por familia: equipos con su estado (fuera de servicio con las órdenes que lo detienen,
  disponible, inactivo, o "no está en SAP" cuando el código legado de Oracle no aparece en SAP),
  más los requeridos por asignación.
- Drill-down por semana: los 7 días con sus números y las órdenes de cada día.
- Funciona en teléfono: la tabla se desplaza dentro de su marco con la columna Familia fija.

## Pantalla Avance de Reparación (Etapa 5)

- Selector de programa (de `VW_ESTRUCTURA_FAMILIAS_PGPM`). Columnas = semanas de calendario del
  programa completo (lunes a domingo, hasta el 10/12); cada familia llena las de su ventana.
- Por familia: Cant. Eq., Inicio, Fin, Total Programado y Total Ejecutado **a la fecha del último
  cálculo**, Diferencia en puntos. Semanas pasadas y la actual: Prog. y Real lado a lado; futuras:
  solo Prog. La tabla abre desplazada hasta la semana actual.
- Curva en "S" (programado vs. real) de la familia elegida; por defecto, la más atrasada.
- Colores de atraso (supuesto, la spec no los define): verde al día o adelantado, naranja hasta 5
  puntos de atraso, rojo más de 5 (`TOLERANCIA_ATRASO` en el frontend).
- No se incluyó el selector "referencia de plan (Real Ejecutado)" ni la columna "Prog. Cierre"
  de la sección 8.3 hasta saber qué significan en el reporte de Oracle (ver pendientes).

## Estado de actualización y alertas (Etapa 6)

- Pantalla Sistema → Estado de actualización: última actualización y si está al día, próxima
  corrida automática, origen de los datos de SAP (simulado o RFC), última alerta; botón
  "Actualizar ahora"; bitácora de las últimas 20 corridas con el detalle de cada paso (registros,
  fecha de corte del delta, error); equipos sin clasificar con el motivo.
- Alerta por correo (sección 11): se revisa cada hora; si pasan más de `MONITOREO.HORAS_ALERTA`
  horas sin una corrida exitosa, se envía un correo a `Alertas:Destinatarios`, una vez por problema
  y repitiendo cada 24 h mientras siga. Cada intento queda en `ALERTA_ENVIADA_PGPM` (también si
  no hay SMTP configurado). En local los correos van a Mailpit (http://localhost:8025); probado.

## Lo que muestran las capturas del reporte de Oracle (30/09/2026) — Etapa 5b

Verificado contra los números de las capturas:
- Las columnas semanales muestran el **% de cada semana, no el acumulado**: Real % en las semanas
  pasadas y la actual (resaltada), Prog. % en las futuras. Total Ejec. = suma de los Real %.
- **T. Prog.** = total de la curva (100, o menos si la curva está incompleta: Tractores 97.67).
- **Prog. Cierre** = programado acumulado hasta la semana anterior.
- **Prog. Act.** = programado para la semana actual.
- **Pend. Ant.** = máx(0, Prog. Cierre − ejecutado hasta la semana anterior). Cabezales:
  66.67 − 56.31 = 10.35.
- **T. Pend. R.** = máx(0, Pend. Ant. + Prog. Act. − Real de la semana actual). Cabezales:
  10.35 + 5.55 − 0 = 15.90; Cosechadoras: 0 + 4.55 − 0.03 = 4.52.
- **El avance real se mide por actividades**, no por horas ni por órdenes cerradas: cada orden
  tiene ~202 actividades agrupadas por sistema (Componentes Genéricos, Eléctrico, Estructura,
  Potencia, Tracción), y Avance % = Días Act. Terminadas / Días Act. Programadas (355.26 / 603.12
  = 59 %). Esto cambia lo que hay que traer de SAP (actividades con su duración, fecha de
  terminación y sistema) y la fórmula provisional actual.
- Hay un drill-down "Análisis de Avance": por equipo (orden, estado, fechas reales, actividades
  totales/terminadas, días programados/terminados, avance %) y por sistema.
- Selectores: Período (zafra, p. ej. "30: 2025-2026") → Reporte (programa); Tipo Calen.
  (Semanal); Ref. Plan. (Real Programado / Real No Programado / Real Ejecutado); Porcentaje o
  Actividad.

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
- [x] Versión del Oracle de producción: **11.2.0.4** (01/10/2026). Ver reglas por ser 11g arriba.
- [x] En la curva planeada, `ACTIVIDAD_PCT` es el % de cada semana (no acumulado); el portal lo
      acumula. Confirmado el 30/09/2026.
- [ ] Fórmulas de "Disponibilidad Comp. %" y "Total Ejec. %" (idealmente el PL/SQL original).
- [ ] Set exacto de estados SAP que cuentan como "fuera de servicio" (hoy: `Liberado,En Proceso`
      en `PARAMETRO_PGPM`).
- [ ] Fecha de arranque de SAP PM (para elegir el período del Sprint 0 y `SAP.FECHA_CARGA_INICIAL`).
- [ ] Criterio numérico de aceptación del Sprint 0.
- [x] Fila "Todas (Promedio)": total disponible / total requerido (30/09/2026).
- [x] Avance: "Prog. Cierre" y opciones de "Ref. Plan." — resuelto con las capturas (ver arriba).
- [ ] Avance: ¿la tolerancia de 5 puntos para naranja/rojo está bien, o taller usa otra?

Responsable: **ABAP** (en curso, ~2 días desde el 30/09)
- [ ] Módulos RFC del Anexo A, campo real del código legado, retención de JCDS.

Responsable: **Basis / Infraestructura**
- [ ] Marcar los 4 módulos como Remote-Enabled, usuario de comunicación con S_RFC, firewall al gateway.
- [ ] Licencia/instalación de SAP .NET Connector (NCo). Ojo: Docker en Windows corre contenedores
      **Linux** por defecto (como en la laptop). Hay que confirmar que NCo corre dentro de un
      contenedor Linux; si no, el job de extracción puede correr como servicio de Windows fuera de
      Docker, o en un contenedor Windows. Se valida al inicio de la Etapa 7.
