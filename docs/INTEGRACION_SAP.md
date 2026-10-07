# Integración con SAP: servicios web de los módulos del ABAP

**Decisión (03/10/2026):** el portal lee SAP por los **servicios web SOAP** que el ABAP generó desde
los módulos de función, no por RFC con SAP .NET Connector. Ventajas: no hace falta descargar ni
licenciar NCo, y todo corre en el mismo contenedor Linux del servidor.

## Servicios (desarrollo, mandante 320 — 08/10/2026)

| Módulo | Uso en el portal |
|---|---|
| `Z_PM_MF_WS_MAESTRO_EQUIPO` | equipos: Ficha, tipo de equipo, grupo de planificación, ubicación técnica |
| `Z_PM_MF_WS_ORDEN_MANTENIMIENTO` | órdenes (disponibilidad, avance, permanencia) |
| `Z_PM_MF_WS_OPERACION_ORDEN` | operaciones (avance por actividades) |
| `Z_PM_MF_WS_AVISO_MANTENIMIENTO` | avisos (parada, inicio de la permanencia) |
| `Z_PM_MF_WS_HISTORIAL_ESTADO` | cambios de estado (horas exactas, pasos de la permanencia) |

URL de cada servicio: `{SapWs:BaseUrl}/sap/bc/srt/rfc/sap/{módulo}/{mandante}/zb_.../zb_...?sap-client={mandante}`
(se arma sola; si algún servicio tiene otra ruta, se configura en `SapWs:Rutas`).

## Qué está hecho

- `SapWebServiceExtractionClient` (DataSourceMode=Production): arma la petición SOAP con la fecha
  "desde" en `yyyymmdd`, autenticación básica, lee la respuesta (cada tabla `<item>` con sus campos)
  y la convierte con **las mismas reglas que el importador del Sprint 0**: estados, Ficha desde la
  descripción, hora de cierre técnico desde el historial, etc. El staging queda igual venga de Excel o
  de SAP.
- Errores claros en la bitácora: usuario o clave rechazados, SOAP Fault con su texto, SAP que no
  responde.
- 5 pruebas automáticas con respuestas SOAP armadas como las de SAP.

## Lo que dicen los WSDL (07/10/2026)

| Módulo | Entrada | Tabla de salida | Campos relevantes |
|---|---|---|---|
| MAESTRO_EQUIPO | — | `ZT_MAESTRO_EQUIPO` | EQUNR, EQKTX, EQART(_DESC), KEY_NUM, INGRP(_DESC), TPLNR(_DESC), ESTADO_EQUIPO |
| ORDEN_MANTENIMIENTO | IV_FECHA_DESDE | `ZT_ORDEN_MANTENIMIENTO` | AUFNR, AUART, ILART, EQUNR, QMNUM, ERDAT+ERFZEIT, GSTR*/GLTR*, FECHA_TECO+HORA_TECO, STATUS_SISTEMA/USUARIO |
| OPERACION_ORDEN | IV_FECHA_DESDE | `ZT_OPERACIONES_O` | AUFNR, VORNR, LTXA1, ARBEI, FSAVD/FSEDD, ISMNW_TOTAL, FECHA_*_CONF, CONF_FINAL, ERNAM, ORIGEN_OPERACION, VPLTY, VPLNR |
| AVISO_MANTENIMIENTO | IV_FECHA_DESDE **e IV_FECHA_HASTA** | `ZT_AVISOS_MANTENIMIENTO` | QMNUM, EQUNR, AUFNR, QMDAT+MZEIT, MSAUS, AUSVN/AUZTV, AUSBS/AUZTB |
| HISTORIAL_ESTADO | IV_FECHA_DESDE | `ZT_HISTORIAL_ESTADO` | AUFNR, OBJNR, STAT, TXT04, TIPO_STATUS, CHGNR, UDATE, UTIME, CHIND, INACT |

- Las fechas son `char8` (`yyyymmdd`) y **ningún parámetro es opcional**: a avisos el portal le
  manda como "hasta" el día siguiente.
- Las tablas son parámetros de salida: no hay que mandarlas en la petición.
- `SOAPAction` = `urn:sap-com:document:sap:rfc:functions:{MÓDULO}:{MÓDULO}Request`.
- El WSDL publica la dirección `http://vhazcdc3ci.sap.cac.com:8000/...`; el portal usa la de https
  (44300). Si Basis prefiere el 8000 para la red interna, se cambia en `SapWs__BaseUrl`.
- **Actividad del plan o agregada:** con `VPLNR` (hoja de ruta de la que se copió la operación) u
  `ORIGEN_OPERACION`, mejor que el usuario creador.

## Pendiente con el ABAP

1. **Suboperación (UVORN) en `Z_PM_MF_WS_OPERACION_ORDEN`.** En las órdenes de reparación (ZC03) cada
   actividad es una suboperación de la operación 0010 (~190 por orden). Sin UVORN, todas llegan con
   VORNR 0010 y el avance por actividades no se puede calcular. Lo mismo para las confirmaciones
   (fechas y horas por suboperación).
2. **Valores de `ORIGEN_OPERACION`** (¿"HOJA_RUTA" / "MANUAL"?), para confirmar la regla.

## Probar contra el SAP de desarrollo (en la laptop)

```
copy deploy\sap-ws.env.example sprint0\sap_ws\sap.env      (completar usuario y clave; está fuera de git)
docker compose -f docker-compose.dev.yml -f docker-compose.sprint0.yml -f docker-compose.sapdev.yml up -d
```

Luego, en el portal: Sistema → Estado de actualización → "Actualizar ahora". La bitácora muestra cuántas
filas trajo cada servicio o el error de SAP. Si falla por el certificado (CA interna de CAC), poner
temporalmente `SapWs__ValidarCertificado=false` en `sap.env` y pedir el certificado de la CA para
instalarlo en el contenedor de producción.

**Seguridad:** la clave solo vive en `sap.env` (laptop) o `.env.prod` (servidor). Para producción,
pedir a Basis un **usuario técnico** (tipo sistema, sin acceso a SAP GUI) con permiso solo para estos
5 servicios.
