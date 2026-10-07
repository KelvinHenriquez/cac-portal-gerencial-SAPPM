# Identidad de los equipos: de la Ficha de Oracle al código de SAP

**Contexto (06/10/2026).** El portal une las órdenes de SAP con la estructura de los reportes
(asignaciones de disponibilidad, OT asignadas a cada familia del programa de reparación) por la
**Ficha** de Oracle (p. ej. `F01168`), que en SAP está en el número de flota (FLEET_NUM). Pero:

- solo 652 de 2.615 equipos de SAP tienen FLEET_NUM;
- los equipos **de fábrica** nunca lo van a tener (no son de flota);
- la intención es ir **eliminando la Ficha**: el código del equipo es el de SAP (EQUNR).

## Regla: la clave de equipo

```
clave de equipo = Ficha            si el equipo la tiene (FLEET_NUM, o escrita en la descripción)
                = código SAP       si no (EQUNR, p. ej. 011-050101-0001)
```

Implementada en un solo lugar (`MapeoEquipos.Clave`) y usada por el job y por todas las pantallas.
Consecuencias:

- Un equipo sin Ficha **se calcula igual**: sus órdenes, avisos y estados se asocian por su código
  SAP.
- La estructura de los reportes puede referirse a un equipo por su Ficha **o** por su código SAP:
  las vistas devuelven una "clave de equipo" en la columna `CODIGO_LEGADO`. Hoy Oracle solo conoce
  Fichas; una estructura que use códigos SAP funciona sin cambiar código.
- "Equipos sin clasificar" = equipos de SAP cuya clave no está en ningún reporte (por ejemplo, los
  motores eléctricos del área industrial). No es un error; es la lista de lo que falta configurar.
- La Ficha tomada de la descripción ("Motobomba John Deere F01198") es una ayuda de transición
  mientras datos maestros no carga FLEET_NUM; cuando la estructura use códigos SAP deja de importar.

## Camino para dejar la Ficha

| Fase | Estructura de los reportes | Clave que usa |
|---|---|---|
| 1 — hoy | Oracle Forms (MAQUINARIAN), leída por las vistas `_PGPM` | Ficha; código SAP para equipos sin Ficha |
| 2 — transición | Oracle Forms; el DBA puede agregar equipos de fábrica por código SAP en una tabla del portal | Ficha o código SAP |
| 3 — sin Oracle | Pantallas de configuración en el portal (reportes, asignaciones, programas), guardadas en tablas `_PGPM` | Código SAP; la Ficha queda solo como dato informativo |

Para la fase 3 se migra la estructura de Oracle una sola vez, traduciendo cada Ficha a su código SAP
con el maestro de equipos. Como las vistas ya son el único punto de entrada de la estructura, solo
cambia de dónde leen (de MAQUINARIAN a las tablas del portal); el cálculo y las pantallas no cambian.
