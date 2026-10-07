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
| 7 | **Alinear ambos reportes con los de Oracle** (capturas y respuestas del 01/10/2026): estructura por período/reporte/asignación, disponibilidad por hora, avance semanal con Prog. Cierre / Pend. Ant. / T. Pend. R., drill-downs como Oracle | **Hecha** |
| 6 | Frescura de datos, monitoreo, alerta > 24 h, pruebas del cálculo | **Hecha** |
| 7b | Ajustes por las respuestas del 02/10/2026: grupos de equipos, OT asignadas a familia, plan por días, disponibilidad solo con correctivas y preventivas | **Hecha** |
| 8 | Integración real: DDL en Oracle de CAC, vistas reales, cliente RFC con NCo ([Anexo A v2](ANEXO_A_V2_ABAP.md)) | 8a (vistas reales) espera la salida de `explorar_maquinarian.sql`; 8b (RFC) espera al ABAP y a Basis |
| 0 | Sprint 0: validar el cálculo con exports reales de SAP ([cómo](SPRINT0_EXPORTS_SAP.md)) | Importador y entorno listos (03/10/2026); esperando los exports de SAP y de MAQUINARIAN |
| 9 | Despliegue en el servidor Docker ([guía](DESPLIEGUE.md)) | Preparado y probado en local (03/10/2026); falta decidir el Docker del servidor, certificado, DNS y el esquema en Oracle de producción |

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

## Respuestas del 01/10/2026 y lo que cambian (Etapa 7)

**Disponibilidad** (capturas "Disponibilidad de Equipos" y "Configuración Reporte de Disponibilidad"):
- Cada reporte (p. ej. "Equipos De Riegos - Gerencial") tiene: umbrales Comp. 1 (rojo, menor a)
  y Comp. 2 (naranja, menor a); su propio calendario de semanas generado de un rango de fechas
  (Sem. 1, 2, ... no semanas ISO); las horas del día en que se mide; y sus **asignaciones** (las
  filas), cada una con su lista de equipos (por grupo o equipos directos), Cant. Asig., Cant. Req.,
  **Disp. Comp. %** (meta configurada, no una fórmula: resuelve ese pendiente), Negrita y Grupo.
- Se mide **por hora**: en cada hora activa se cuentan los equipos disponibles; el día es el
  promedio de sus horas y la semana el de sus días.
- **Potencial % = disponibles / requeridos, sin tope** (la fila Todas muestra 101 %). Mi pantalla
  lo calculaba sobre el total de equipos: se corrige. Disponibilidad % sigue topada en 100.
- Colores como Oracle: texto rojo bajo Comp. 1, naranja bajo Comp. 2, negro desde ahí.
- Detalle por asignación y semana: por día Disp. Real %, Disp. Comp. %, Hrs al día, Hrs OT
  Planificada / Preventivo / Correctiva, Hrs Real Efec. (= Hrs al día − Correctiva, según los
  números), resumen de la semana, disponibilidad hora por hora y las órdenes con su observación.
- **Fuera de servicio desde que la orden se crea** (no desde el inicio real) hasta su cierre.
  Para correctivas, tener en cuenta el **aviso** (en SAP el aviso indica si hay parada del equipo).

**Avance** (capturas "Avance de Órdenes de Trabajo" y "Configuración Reporte Avance"):
- **Período de reparación** (p. ej. "30: 2025-2026") → varios **reportes** (Programa de
  Reparación 2026, ... Producción, Cosecha, Varios, Nivelación, Preparación, Maq. Pesada), cada
  uno con su calendario, sus familias y su plan semanal por familia en **actividades y días**.
- Columnas semanales con el % **de cada semana** y, a pedido, un modo **acumulado a la fecha** para
  comparar.
- Actividad = operación de la orden; terminada cuando tiene horas reales; cuenta en la semana en
  que se confirman; sus días salen de las fechas plan inicio/fin. Ref. Plan: Real Programado
  (actividades del plan original) + Real No Programado (agregadas en la reparación) = Real
  Ejecutado.
- **Cómo medir el real:** el área prefiere el % por **órdenes** (lo que cuenta en la práctica es
  cuándo se cierran); por actividades puede esconder ineficiencias. Se ofrecerán los dos, con
  órdenes por defecto.
- Colores como Oracle: sin rojo/naranja; se resalta la semana actual.

**Otros:** SAP PM en vivo desde el 01/06/2026, estable desde julio (`SAP.FECHA_CARGA_INICIAL` =
2026-06-01; Sprint 0 con julio a septiembre). El correo de alertas queda configurable y apagado
en producción hasta que se decida (sin destinatarios no envía nada, solo registra).

## Qué quedó en la Etapa 7

- **Estructura (Oracle Forms):** 10 vistas con la forma real de las pantallas de configuración
  (plantilla para producción en [oracle/vistas_produccion.sql](oracle/vistas_produccion.sql)).
  Disponibilidad: reporte → semanas, horas y asignaciones → equipos. Avance: período → reportes →
  familias → plan semanal (actividades y días).
- **Staging SAP según el Anexo A v2:** órdenes con hora de creación y de cierre técnico, clase de
  actividad y aviso; operaciones (actividades) con fechas plan, fecha de confirmación,
  sistema/sub-sistema/componente y programada/no programada; avisos con parada e inicio/fin de
  avería. El simulador local genera además preventivos (ZC02) y correctivos (ZC01) con aviso.
- **Disponibilidad:** una foto por cada hora activa del reporte; un equipo cuenta fuera desde la
  creación de su orden hasta el cierre técnico (las correctivas con aviso, solo si el aviso indica
  parada y desde/hasta la avería). Horas por tipo de orden con prioridad correctiva > preventiva >
  planificada; Hrs Real Efec. = Hrs al día − correctiva. Potencial sin tope. Colores como Oracle
  (rojo / naranja / negro). Las columnas actuales son la foto de la última hora medida antes de la
  última actualización. Drill-downs: "Disponibilidad Semanal" (matriz por día, resumen, hora por
  hora, órdenes con observación) y equipos de la asignación.
- **Avance:** % de cada semana (no acumulado) y vista Acumulado; real por órdenes (por defecto) o
  por actividades con Ref. Plan.; T. Prog., Prog. Cierre, Total Ejec., Prog. Act., Pend. Ant. y
  T. Pend. R. (pruebas con los números de las capturas); semana actual resaltada, sin colores de
  atraso. Drill-downs: "Avance Semanal" (resumen, días, actividades terminadas por sistema,
  órdenes cerradas) y "Análisis de Avance" por equipo y por sistema.
- Todo probado en local contra Oracle 11g XE: corrida completa (~14 s), 40 pruebas automáticas,
  pantallas verificadas en el navegador (escritorio y teléfono).

## Respuestas del 02/10/2026 y lo que cambian (Etapa 7b)

- **% programado por días**, no por cantidad de actividades (`AVANCE.BASE_PLAN=DIAS`).
- La Fecha Est. de la familia no se usa: el plan es lo distribuido por semana (julio a diciembre).
- Dentro de su familia todas las órdenes pesan igual (ya era así en la medida por órdenes).
- **El programa de cada familia son las OT asignadas** ("Órdenes de Trabajo Asignada a Familia"):
  vista nueva `VW_AVANCE_OT_PGPM`. Una orden de SAP entra al programa si su equipo está entre los
  asignados; si el reporte no tiene OT asignadas, se cae a la regla anterior (familia + ventana).
- **Disponibilidad: cuentan correctivas y preventivas** (`DISPONIBILIDAD.CLASES_ORDEN=ZC01,ZC02`).
  Las predictivas (ZC03) son el programa de reparación del Centro de Servicios y no sacan el
  equipo de la disponibilidad.
- Hrs Real Efec. = Hrs al día − correctiva: confirmado. Columnas actuales = última hora medida:
  confirmado.
- **Grupo** = equipos de distinta naturaleza que juntos resuelven una operación (p. ej. "Aguacatico
  Goteo"). La fila de grupo va seguida de las filas de sus equipos: se muestran con sangría y
  "Todas" cuenta solo el grupo (si no, se contaría doble).
- La tabla de equipos trae Ficha (`F01168`) y Equipo (`A29017-002`): la vista de equipos suma
  `EQUIPO_CODIGO` y el detalle de equipos lo muestra.
- Oracle sigue en paralelo (en teoría): sirve para comparar en el Sprint 0.
- Tablas reales: están en el esquema **MAQUINARIAN**. Con los nombres de bloques de las pantallas
  se armó [oracle/explorar_maquinarian.sql](oracle/explorar_maquinarian.sql), que solo consulta el
  diccionario de datos; lo corre Kelvin con su usuario y con la salida se llenan las vistas reales.
  El portal no usa su usuario personal ni se conecta a producción desde desarrollo.
- Verificado en local (base nueva, corrida OK, 41 pruebas): "Detallado P30" con grupos y miembros,
  Todas 21/21/21; Centro de Servicios Todas 219/191/211; Tractores 40 equipos en el Programa de
  Reparación y 4 en Nivelación.

## Lo que salió de la exploración de MAQUINARIAN y las respuestas de la noche del 02/10/2026

- **Tablas reales** (diccionario exportado en `oracle/1.xlsx` a `5.xlsx`): disponibilidad en
  `MA_CONFIG_DISPONIBILIDAD` → `MA_CONFIG_SEMANA_DISP`, `MA_CONFIG_HORA_DISP`,
  `MA_EQUIPOS_REQ_X_FAMILIA` (asignaciones) → `MA_DEQUIPOS_REQ_X_FAMILIA` (equipos); avance en
  `MA_ENC_CONFIG_AVANCE` → `MA_DET_CONFIG_AVANCE` (familias) → `MA_SUBDET_CONFIG_AVANCE` (plan por
  semana) y `MA_OT_CONFIG_AVANCE` (OT asignadas, una fila por actividad); calendario en
  `MA_ENC_SEMANA_OT` / `MA_DET_SEMANA_OT`; equipos en `MA_MAESTRO_EQUIPOS` (FICHA, EQUIPO).
- [oracle/vistas_produccion.sql](oracle/vistas_produccion.sql) ya está escrito sobre esas tablas
  (sin huecos `<...>`), compilado y probado en el Oracle local contra una réplica vacía con las
  columnas exactas (`db/oracle/pruebas/`). Quedan 6 puntos "VERIFICAR" que se cierran con
  [oracle/explorar_maquinarian_2.sql](oracle/explorar_maquinarian_2.sql) (también solo SELECT).
- En Oracle los reportes de disponibilidad se identifican por área de servicio + tipo de reporte:
  el portal usa `REPORTE_ID = ID_AREA_SERVICIO * 1000 + ID_TIPO_REPORTE`.
- **Oracle ya guarda sus resultados** (`MA_DISPONIBILIDAD_EQUIPOS` por hora, `MA_TEMP_DISPONIBILIDAD`,
  `MA_RES_CONFIG_AVANCE`, columnas `REAL_*` de `MA_SUBDET_CONFIG_AVANCE`): sirven para comparar el
  portal contra Oracle número por número en el Sprint 0, y el PL/SQL que los llena trae las fórmulas.
- **Código legado = FLEET_NUM** (la Ficha). Los equipos que no usan combustible quedaron sin código
  legado en SAP: no se pueden cruzar (observación 2 del [Anexo A v2](ANEXO_A_V2_ABAP.md)).
- **Grupos:** debajo de un grupo no van necesariamente sus equipos. El portal ya no usa la posición:
  una fila cuenta como incluida en un grupo solo si todos sus equipos están en ese grupo; se
  muestra con la nota "Incluido en …" y no entra en "Todas".
- **Actividad programada vs. agregada:** la decide el usuario que creó la operación en SAP (el de
  planificación = hoja de ruta). Nuevo parámetro `SAP.USUARIOS_HOJA_RUTA` (falta el nombre del
  usuario) y columna `USUARIO_CREACION` en `STG_SAP_OPERACION_PGPM`.
- El ABAP entregó equipos y avisos (`Z_PM_MF_WS_*`); revisión en el Anexo A v2.

## PL/SQL de Oracle y ajustes del 03/10/2026

- Empresa de CAC = **30**. Hora configurada como texto `' 00:00'` (24 h). 22 reportes de
  disponibilidad, 7 activos (51 a 57); 7 programas de reparación del período 30 (correlativos 27 a 33).
- Fórmulas reales de Oracle y diferencias con el portal: [oracle/ANALISIS_PLSQL_ORACLE.md](oracle/ANALISIS_PLSQL_ORACLE.md).
- Vistas de producción ajustadas a lo que hace Oracle: fichas repetidas (se queda la activa),
  fichas "000" fuera, asignaciones sin requeridos fuera, hora en 24 h, familia desde MA_MST_FAMILIA,
  actividades SELECCIONADO = 'S'. Probadas otra vez contra la réplica (11 vistas válidas).
- Nuevo parámetro `DISPONIBILIDAD.BASE_PORCENTAJE` (REQUERIDOS por defecto; EQUIPOS = como Oracle).

## Respuestas del 03/10/2026

- Base del % de disponibilidad y clases de orden que cuentan: **configurables**. Nueva pantalla
  **Sistema → Parámetros**: todos ven las reglas; solo los usuarios de `Admin:Usuarios` las
  cambian, con validación y registro de quién y cuándo.
- Actividad **no programada = agregada por otro usuario**; una actividad **no trabajada no cuenta
  como hecha** (el portal exige horas reales). Se mantiene lo construido.
- Oracle se sigue alimentando hasta que el portal lo reemplace: la comparación del Sprint 0 vale
  para ambos reportes.
- Servidor: el mismo de Pago en Efectivo (Docker Desktop 4.93, contenedores Linux). El portal sale
  por **8443** porque el 443 es de Pago en Efectivo. Ver [DESPLIEGUE.md](DESPLIEGUE.md).
- `PG_PERIODO` es de **PARGER**: las vistas leen `PARGER.PG_PERIODO`.
- Para el ABAP: [ANEXO_A_V3_ABAP.md](ANEXO_A_V3_ABAP.md), especificación narrativa por módulo.

## Sprint 0, parte Oracle (06/10/2026)

Cargada la configuración real (export de MAQUINARIAN y PARGER, 140.000 filas) y los resultados de
Oracle de julio a septiembre. Con las vistas de producción:

- **Disponibilidad:** 256 de 256 asignaciones activas iguales a Oracle en equipos y requeridos
  (reportes 51 a 57). Las 6 que Oracle midió y el portal no son de equipos desactivados el 25/09
  (plantas eléctricas J08022 y J08026, góndola C13002); Oracle también dejó de medirlas ese día.
- Ajustes para lograrlo: el estado activo se mira en el registro exacto del equipo (no en la
  ficha, que puede estar repetida) y las asignaciones sin equipos activos no se muestran, como hace
  Oracle.
- **Avance:** 7 programas de 2026 con sus familias y OT asignadas; el plan semanal coincide con las
  actividades de las OT.
- Hallazgos en Oracle: fuera de servicio solo con correctiva (tipo 1) en estado Creada, En Proceso
  o Terminada; **"En Espera" cuenta como disponible**. Midió 88 de 92 días (4 sin proceso) y
  algunos días más de 24 veces.
- Falta la parte SAP para comparar los porcentajes día por día.

## Sprint 0, parte SAP (06/10/2026)

Exports de SAP de julio a septiembre: maestro y avisos (módulos del ABAP), IW39, IW47 y JCDS (falta
IW37N). Corrida completa OK con datos reales. Resultado en `sprint0/Comparacion_Sprint0_Disponibilidad.xlsx`
(fuera de git: datos reales).

- **Cruce SAP ↔ Oracle:** 88 a 100 % de los equipos de cada reporte encontrados en SAP. Solo 652
  de 2.615 equipos traen KEY_NUM; para 582 más la Ficha se tomó de la descripción ("Motobomba John
  Deere F01198"). Las 1.391 correctivas y preventivas tienen equipo con Ficha.
- **Por reporte** el portal queda cerca de Oracle (con las reglas de Oracle, 4 de 7 a 1,5 puntos o
  menos). **Por asignación y día** solo el 64 % de los días queda a 5 puntos o menos: las órdenes
  de los dos sistemas no coinciden. 67 asignaciones tienen correctivas abiertas en SAP que Oracle no
  tiene; 41, OT abiertas en Oracle que SAP no tiene; 67, órdenes en los dos con fechas distintas.
- **Datos a corregir:** 64 correctivas SAP abiertas 30 días o más (13 sin cierre técnico al 06/10,
  algunas desde julio) y 250 OT de Oracle abiertas 30 días o más en el trimestre.
- Ajustes al importador: etiquetas reales de IW39/IW47/JCDS, Ficha desde la descripción, petición
  de borrado = anulada, cierre = primera activación de CTEC (o CERR).

## Decisiones del 06/10/2026

- **Reglas de Oracle por defecto:** % sobre equipos, solo correctivas (ZC01), sin la regla del
  aviso. Se pueden cambiar en Sistema → Parámetros.
- **Criterio de aceptación** semanal (reporte ±3 puntos en 90 % de las semanas; asignación ±5 en
  80 %; toda diferencia explicada): ver [SPRINT0_EXPORTS_SAP.md](SPRINT0_EXPORTS_SAP.md), sección D.
  Hoy no se cumple por diferencias de datos entre SAP y Oracle.
- **Clave de equipo = Ficha o código SAP:** los equipos de fábrica nunca tendrán FLEET_NUM y la
  Ficha se va a eliminar. Ver [IDENTIDAD_EQUIPOS.md](IDENTIDAD_EQUIPOS.md).
- **Integración con SAP:** el ABAP tiene los módulos y sus WSDL (servicios web SOAP).
- **Avance por órdenes con la fecha de fin real** (parámetro `AVANCE.FECHA_ORDEN_TERMINADA`), no la
  del cierre técnico: el taller cerró técnicamente 32 órdenes en lote el 25/08 aunque el trabajo
  terminó en julio y agosto. Con fin real, Vagones reparte su avance en las semanas 1 a 5 y 10.

## Reporte 3: Tiempo de Permanencia (06/10/2026)

Pedido: cuánto tarda un equipo (o en promedio su familia) desde que se abre el aviso hasta que se
cierra la orden, con el detalle de cada paso; por área (Centro de Servicios / Industrial) →
familia → equipo → órdenes. Solo correctivas.

- **Reglas acordadas:** fin = orden **finalizada** (estado de usuario FINL; si no, fin real); el
  cierre técnico se muestra aparte (cierre administrativo). Correctivas ZC01 (Centro de Servicios) y
  **ZI01** (Industrial). Área por **grupo de planificación** del equipo (CDS / IND). Familia =
  **tipo de equipo de SAP** (EQART). Promedios solo con órdenes finalizadas en el período (por
  defecto, los últimos 3 meses); las abiertas se muestran "en curso" con su antigüedad. Promedio y
  mediana. Días de calendario.
- **Pasos:** respuesta (aviso → orden), liberación (→ LIB), espera de inicio (→ ENPR), ejecución
  (→ FINL). **Espera de repuestos:** parámetro `PERMANENCIA.ESTADO_ESPERA` para cuando exista un
  estado de usuario en SAP; mide el tiempo en ese estado aunque entre y salga varias veces.
- Staging: el equipo guarda tipo de equipo, grupo de planificación y **ubicación técnica** (para el
  futuro reporte por ubicación del área industrial); el historial de estados guarda si el cambio dejó
  el estado inactivo. Resultado en `CALC_PERMANENCIA_PGPM` (una fila por orden, se recalcula entera).
- Con los datos reales (julio a septiembre): Centro de Servicios, 1.161 correctivas finalizadas en
  447 equipos, **6,5 días promedio y 2,0 de mediana**; el mayor peso está en la ejecución (4,2 d) y
  la liberación (2,1 d). Industrial: solo 3 ZI01 finalizadas y 28 en curso.

## Integración con SAP por servicios web (08/10/2026)

El ABAP publicó los 5 servicios web en desarrollo (mandante 320). El cliente SOAP del portal está
hecho y probado con respuestas armadas; falta confirmar contra los WSDL el parámetro de fecha y las
tablas de salida, y la primera prueba real (la corre Kelvin con su usuario en un archivo local). Ver
[INTEGRACION_SAP.md](INTEGRACION_SAP.md). El cliente RFC/NCo se descartó.

## Avance por actividades con IW37N (07/10/2026)

- En SAP, cada orden de reparación (ZC03) tiene **una operación (0010) con ~190 suboperaciones**:
  **cada suboperación es una actividad**. El portal las guarda como "0010/0005"; la operación madre
  no cuenta aparte. Las confirmaciones de IW47 se cruzan por operación y suboperación.
- Las suboperaciones traen **la misma fecha plan de inicio y fin**, así que la duración en días da 0.
  Nuevo parámetro `AVANCE.PESO_ACTIVIDAD`: **CANTIDAD** por defecto (cada actividad vale 1, como
  Oracle), HORAS_PLAN o DIAS. El % programado sigue saliendo del plan de Oracle por días.
- Resultado (Programa de Reparación 2026): 64.912 actividades; con horas reales en SAP solo el 31 %.
  El portal da menos avance que Oracle (p. ej. Tractores 28 % vs. 90 %, Cabezales 4 % vs. 61 %,
  Vagones 50 % vs. 55 %) porque **el taller registra el avance en Oracle y confirma poco en SAP**.
  Mientras dure el paralelo, el avance del portal será tan completo como las confirmaciones en SAP.

## Qué sigue

1. **8a — Vistas reales:** escritas y probadas en local. Falta correr
   `explorar_maquinarian_2.sql` (Kelvin) para cerrar los puntos "VERIFICAR".
2. **Sprint 0:** importador de Excel (`DataSourceMode=Excel`) y entorno aparte
   (`docker-compose.sprint0.yml`) listos y probados de punta a punta con datos sintéticos. Faltan
   los exports reales de SAP y de la configuración de MAQUINARIAN ([cómo](SPRINT0_EXPORTS_SAP.md)),
   y definir la comparación contra los resultados de Oracle cuando llegue el PL/SQL.
3. **8b — RFC real (depende del ABAP y de Basis):** cliente NCo contra los 5 módulos del Anexo A v2
   (confirmado RFC, no servicio web, el 03/10/2026). La conversión de estados y números de SAP ya
   está compartida con el importador (`EstadosSap`).
4. **9 — Despliegue:** imágenes de producción, HTTPS, healthcheck `/api/salud` y guía en
   [DESPLIEGUE.md](DESPLIEGUE.md), probados en la laptop. Falta el servidor.

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
- [x] Correr `oracle/explorar_maquinarian.sql` (02/10/2026).
- [ ] Correr `oracle/explorar_maquinarian_2.sql` y exportar cada resultado (6.xlsx en adelante).
- [x] Código legado: la Ficha, en FLEET_NUM.
- [ ] Equipos sin combustible sin código legado en SAP: ¿se corrigen en SAP o se busca otro cruce?
- [ ] Usuario(s) SAP de planificación que crean la hoja de ruta (para `SAP.USUARIOS_HOJA_RUTA`).
- [ ] Confirmar la regla de grupos de arriba para "Todas".
- [ ] Esquema/tablespace real para los objetos `_pgpm` y usuario técnico del portal con CREATE TABLE,
      CREATE VIEW, CREATE SEQUENCE (o correr el script de migraciones una vez y dejarle solo DML).
- [ ] Nombres reales de las tablas del Sistema de Órdenes de Trabajo: familias, curva por semana,
      equipos requeridos, umbrales — **y la tabla de equipos (Objeto Técnico / Ficha) con su familia**,
      que la spec no lista pero hace falta para el cruce por código legado y el detalle por familia.
- [x] Versión del Oracle de producción: **11.2.0.4** (01/10/2026). Ver reglas por ser 11g arriba.
- [x] En la curva planeada, `ACTIVIDAD_PCT` es el % de cada semana (no acumulado); el portal lo
      acumula. Confirmado el 30/09/2026.
- [x] "Disponibilidad Comp. %" es una meta configurada por asignación; "Total Ejec. %" = suma del real semanal.
- [x] Fuera de servicio: desde la creación de la orden hasta su cierre técnico; clases ZC01 y ZC02.
- [x] Fecha de arranque de SAP PM: 01/06/2026, estable desde julio.
- [ ] Criterio numérico de aceptación del Sprint 0.
- [x] Fila "Todas (Promedio)": total disponible / total requerido (30/09/2026).
- [x] Avance: "Prog. Cierre" y opciones de "Ref. Plan." — resuelto con las capturas (ver arriba).
- [x] Avance sin colores de atraso, como Oracle (solo se resalta la semana actual).

Responsable: **ABAP** (en curso, ~2 días desde el 30/09)
- [ ] Módulos RFC del Anexo A v2, campo real del código legado, retención de JCDS.
- [ ] Cómo distinguir en SAP una actividad del plan original (hoja de ruta) de una agregada durante
      la reparación (probablemente por fecha plan distinta a la del paquete).

Responsable: **Basis / Infraestructura**
- [ ] Marcar los 5 módulos como Remote-Enabled, usuario de comunicación con S_RFC, firewall al gateway.
- [ ] Licencia/instalación de SAP .NET Connector (NCo). Ojo: Docker en Windows corre contenedores
      **Linux** por defecto (como en la laptop). Hay que confirmar que NCo corre dentro de un
      contenedor Linux; si no, el job de extracción puede correr como servicio de Windows fuera de
      Docker, o en un contenedor Windows. Se valida al inicio de la Etapa 8b.
