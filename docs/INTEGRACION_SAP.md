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

## Qué falta (depende de los WSDL)

Los WSDL piden usuario y clave, así que los descarga Kelvin (no Claude): abrir cada URL en el
navegador, entrar con el usuario de SAP y guardar la página (Ctrl+S) en `sprint0\sap_ws\` con el
nombre del módulo (`Z_PM_MF_WS_MAESTRO_EQUIPO.wsdl`, ...). Con ellos se confirma:

1. el nombre del parámetro de entrada "fecha desde" (hoy se asume `IV_FECHA_DESDE`);
2. si las tablas de salida hay que mandarlas vacías en la petición (`SapWs:TablasEnPeticion`);
3. los nombres de campo de cada tabla de salida contra el [Anexo A v3](ANEXO_A_V3_ABAP.md).

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
