using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ModeloInicialPgpm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ALERTA_ENVIADA_PGPM",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", unicode: false, maxLength: 36, nullable: false),
                    TIPO = table.Column<string>(type: "VARCHAR2(40)", unicode: false, maxLength: 40, nullable: false),
                    FECHA = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    DESTINATARIOS = table.Column<string>(type: "VARCHAR2(1000)", unicode: false, maxLength: 1000, nullable: true),
                    ASUNTO = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: false),
                    ENVIADA = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    ERROR = table.Column<string>(type: "VARCHAR2(2000)", unicode: false, maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ALERTA_ENVIADA_PGPM", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "CALC_AVANCE_SEMANAL_PGPM",
                columns: table => new
                {
                    REPORTE_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    FAMILIA_CODIGO = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: false),
                    NO_SEMANA = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    FECHA_INICIO = table.Column<DateTime>(type: "DATE", nullable: false),
                    FECHA_FIN = table.Column<DateTime>(type: "DATE", nullable: false),
                    PROG_PCT = table.Column<decimal>(type: "DECIMAL(6,2)", precision: 6, scale: 2, nullable: false),
                    ORDENES_TOTAL = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ORDENES_CERRADAS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    REAL_ORDENES_PCT = table.Column<decimal>(type: "DECIMAL(6,2)", precision: 6, scale: 2, nullable: false),
                    ACT_TOTAL = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ACT_TERMINADAS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DIAS_ACT_TOTAL = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    DIAS_ACT_TERMINADAS = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    REAL_ACT_PCT = table.Column<decimal>(type: "DECIMAL(6,2)", precision: 6, scale: 2, nullable: false),
                    REAL_ACT_PROG_PCT = table.Column<decimal>(type: "DECIMAL(6,2)", precision: 6, scale: 2, nullable: false),
                    REAL_ACT_NO_PROG_PCT = table.Column<decimal>(type: "DECIMAL(6,2)", precision: 6, scale: 2, nullable: false),
                    CORRIDA_ID = table.Column<string>(type: "VARCHAR2(36)", unicode: false, maxLength: 36, nullable: false),
                    GENERADO_EN = table.Column<DateTime>(type: "DATE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CALC_AVANCE_SEMANAL_PGPM", x => new { x.REPORTE_ID, x.FAMILIA_CODIGO, x.NO_SEMANA });
                });

            migrationBuilder.CreateTable(
                name: "CALC_DISP_DIARIA_PGPM",
                columns: table => new
                {
                    REPORTE_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ASIGNACION_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    FECHA = table.Column<DateTime>(type: "DATE", nullable: false),
                    CANT_EQUIPO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EQ_REQUERIDO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EQ_DISPONIBLE_PROM = table.Column<decimal>(type: "DECIMAL(10,2)", precision: 10, scale: 2, nullable: false),
                    DISP_REAL_PCT = table.Column<decimal>(type: "DECIMAL(6,2)", precision: 6, scale: 2, nullable: false),
                    POTENCIAL_PCT = table.Column<decimal>(type: "DECIMAL(7,2)", precision: 7, scale: 2, nullable: false),
                    DISP_COMP_PCT = table.Column<decimal>(type: "DECIMAL(6,2)", precision: 6, scale: 2, nullable: false),
                    HORAS_MEDIDAS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    HRS_DIA = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    HRS_OT_PLANIFICADA = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    HRS_OT_PREVENTIVO = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    HRS_OT_CORRECTIVA = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    HRS_REAL_EFEC = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    HRS_DIA_DISP_COMP = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    CORRIDA_ID = table.Column<string>(type: "VARCHAR2(36)", unicode: false, maxLength: 36, nullable: false),
                    GENERADO_EN = table.Column<DateTime>(type: "DATE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CALC_DISP_DIARIA_PGPM", x => new { x.REPORTE_ID, x.ASIGNACION_ID, x.FECHA });
                });

            migrationBuilder.CreateTable(
                name: "CALC_DISP_SEMANAL_PGPM",
                columns: table => new
                {
                    REPORTE_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ASIGNACION_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    NO_SEMANA = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    FECHA_INICIO = table.Column<DateTime>(type: "DATE", nullable: false),
                    FECHA_FIN = table.Column<DateTime>(type: "DATE", nullable: false),
                    DIAS_CALCULADOS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CANT_EQUIPO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EQ_REQUERIDO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EQ_DISPONIBLE_PROM = table.Column<decimal>(type: "DECIMAL(10,2)", precision: 10, scale: 2, nullable: false),
                    DISP_REAL_PCT = table.Column<decimal>(type: "DECIMAL(6,2)", precision: 6, scale: 2, nullable: false),
                    DISP_REAL_MIN_PCT = table.Column<decimal>(type: "DECIMAL(6,2)", precision: 6, scale: 2, nullable: false),
                    POTENCIAL_PCT = table.Column<decimal>(type: "DECIMAL(7,2)", precision: 7, scale: 2, nullable: false),
                    DISP_COMP_PCT = table.Column<decimal>(type: "DECIMAL(6,2)", precision: 6, scale: 2, nullable: false),
                    HRS_DIA = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    HRS_OT_PLANIFICADA = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    HRS_OT_PREVENTIVO = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    HRS_OT_CORRECTIVA = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    HRS_REAL_EFEC = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    HRS_DIA_DISP_COMP = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    CORRIDA_ID = table.Column<string>(type: "VARCHAR2(36)", unicode: false, maxLength: 36, nullable: false),
                    GENERADO_EN = table.Column<DateTime>(type: "DATE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CALC_DISP_SEMANAL_PGPM", x => new { x.REPORTE_ID, x.ASIGNACION_ID, x.NO_SEMANA });
                });

            migrationBuilder.CreateTable(
                name: "CALC_EQUIPO_SIN_CLASIF_PGPM",
                columns: table => new
                {
                    EQUIPO_SAP_ID = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: false),
                    CODIGO_LEGADO = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: true),
                    DESCRIPCION = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: true),
                    PUESTO_TRABAJO = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: true),
                    MOTIVO = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: false),
                    CANT_ORDENES = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CORRIDA_ID = table.Column<string>(type: "VARCHAR2(36)", unicode: false, maxLength: 36, nullable: false),
                    DETECTADO_EN = table.Column<DateTime>(type: "DATE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CALC_EQ_SIN_CLASIF_PGPM", x => x.EQUIPO_SAP_ID);
                });

            migrationBuilder.CreateTable(
                name: "LOG_EXTRACCION_PGPM",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", unicode: false, maxLength: 36, nullable: false),
                    CORRIDA_ID = table.Column<string>(type: "VARCHAR2(36)", unicode: false, maxLength: 36, nullable: false),
                    FUENTE = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: false),
                    INICIO = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    FIN = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    ESTADO = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    REGISTROS_PROCESADOS = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    FECHA_CORTE_DESDE = table.Column<DateTime>(type: "DATE", nullable: true),
                    ORIGEN = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: true),
                    ERROR_MENSAJE = table.Column<string>(type: "CLOB", unicode: false, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOG_EXTRACCION_PGPM", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PARAMETRO_PGPM",
                columns: table => new
                {
                    CLAVE = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: false),
                    VALOR = table.Column<string>(type: "VARCHAR2(500)", unicode: false, maxLength: 500, nullable: false),
                    DESCRIPCION = table.Column<string>(type: "VARCHAR2(500)", unicode: false, maxLength: 500, nullable: true),
                    ACTUALIZADO_EN = table.Column<DateTime>(type: "DATE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PARAMETRO_PGPM", x => x.CLAVE);
                });

            migrationBuilder.CreateTable(
                name: "STG_SAP_AVISO_PGPM",
                columns: table => new
                {
                    AVISO_SAP_ID = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    CLASE_AVISO = table.Column<string>(type: "VARCHAR2(10)", unicode: false, maxLength: 10, nullable: true),
                    TEXTO = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: true),
                    EQUIPO_SAP_ID = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: true),
                    ORDEN_SAP_ID = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: true),
                    FECHA_AVISO = table.Column<DateTime>(type: "DATE", nullable: false),
                    PARADA = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    INICIO_AVERIA = table.Column<DateTime>(type: "DATE", nullable: true),
                    FIN_AVERIA = table.Column<DateTime>(type: "DATE", nullable: true),
                    STATUS_SISTEMA = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: true),
                    FECHA_CIERRE = table.Column<DateTime>(type: "DATE", nullable: true),
                    ACTUALIZADO_EN = table.Column<DateTime>(type: "DATE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STG_AVISO_PGPM", x => x.AVISO_SAP_ID);
                });

            migrationBuilder.CreateTable(
                name: "STG_SAP_EQUIPO_PGPM",
                columns: table => new
                {
                    EQUIPO_SAP_ID = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: false),
                    CODIGO_LEGADO = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: true),
                    DESCRIPCION = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: true),
                    PUESTO_TRABAJO = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: true),
                    INACTIVO = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    ACTUALIZADO_EN = table.Column<DateTime>(type: "DATE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STG_EQUIPO_PGPM", x => x.EQUIPO_SAP_ID);
                });

            migrationBuilder.CreateTable(
                name: "STG_SAP_HIST_ESTADO_PGPM",
                columns: table => new
                {
                    ORDEN_SAP_ID = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    ESTADO_CODIGO = table.Column<string>(type: "VARCHAR2(10)", unicode: false, maxLength: 10, nullable: false),
                    NUMERO_CAMBIO = table.Column<string>(type: "VARCHAR2(10)", unicode: false, maxLength: 10, nullable: false),
                    ESTADO_TEXTO = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: true),
                    FECHA_HORA_CAMBIO = table.Column<DateTime>(type: "DATE", nullable: false),
                    INDICADOR = table.Column<string>(type: "VARCHAR2(1)", unicode: false, maxLength: 1, nullable: false),
                    ACTUALIZADO_EN = table.Column<DateTime>(type: "DATE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STG_HIST_ESTADO_PGPM", x => new { x.ORDEN_SAP_ID, x.ESTADO_CODIGO, x.NUMERO_CAMBIO });
                });

            migrationBuilder.CreateTable(
                name: "STG_SAP_ORDEN_PGPM",
                columns: table => new
                {
                    ORDEN_SAP_ID = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    CLASE_ORDEN = table.Column<string>(type: "VARCHAR2(10)", unicode: false, maxLength: 10, nullable: false),
                    CLASE_ACTIVIDAD = table.Column<string>(type: "VARCHAR2(10)", unicode: false, maxLength: 10, nullable: true),
                    EQUIPO_SAP_ID = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: true),
                    DESCRIPCION = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: true),
                    PUESTO_TRABAJO = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: true),
                    AVISO_SAP_ID = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: true),
                    ESTADO_ORDEN = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: false),
                    STATUS_SISTEMA = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: true),
                    STATUS_USUARIO = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: true),
                    FECHA_CREACION = table.Column<DateTime>(type: "DATE", nullable: false),
                    FECHA_INICIO_PLAN = table.Column<DateTime>(type: "DATE", nullable: true),
                    FECHA_FIN_PLAN = table.Column<DateTime>(type: "DATE", nullable: true),
                    FECHA_INICIO_REAL = table.Column<DateTime>(type: "DATE", nullable: true),
                    FECHA_FIN_REAL = table.Column<DateTime>(type: "DATE", nullable: true),
                    FECHA_CIERRE = table.Column<DateTime>(type: "DATE", nullable: true),
                    ACTUALIZADO_EN = table.Column<DateTime>(type: "DATE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STG_ORDEN_PGPM", x => x.ORDEN_SAP_ID);
                    table.ForeignKey(
                        name: "FK_STG_ORDEN_EQUIPO_PGPM",
                        column: x => x.EQUIPO_SAP_ID,
                        principalTable: "STG_SAP_EQUIPO_PGPM",
                        principalColumn: "EQUIPO_SAP_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "STG_SAP_OPERACION_PGPM",
                columns: table => new
                {
                    ORDEN_SAP_ID = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    OPERACION = table.Column<string>(type: "VARCHAR2(10)", unicode: false, maxLength: 10, nullable: false),
                    TEXTO = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: true),
                    PUESTO_TRABAJO = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: true),
                    SISTEMA = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: true),
                    SUBSISTEMA = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: true),
                    COMPONENTE = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: true),
                    TRABAJO_PLAN_HH = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    TRABAJO_REAL_HH = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    FECHA_INICIO_PLAN = table.Column<DateTime>(type: "DATE", nullable: true),
                    FECHA_FIN_PLAN = table.Column<DateTime>(type: "DATE", nullable: true),
                    FECHA_PRIMERA_CONF = table.Column<DateTime>(type: "DATE", nullable: true),
                    FECHA_ULTIMA_CONF = table.Column<DateTime>(type: "DATE", nullable: true),
                    CONF_FINAL = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    PROGRAMADA = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    ACTUALIZADO_EN = table.Column<DateTime>(type: "DATE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STG_OPERACION_PGPM", x => new { x.ORDEN_SAP_ID, x.OPERACION });
                    table.ForeignKey(
                        name: "FK_STG_OPERACION_ORDEN_PGPM",
                        column: x => x.ORDEN_SAP_ID,
                        principalTable: "STG_SAP_ORDEN_PGPM",
                        principalColumn: "ORDEN_SAP_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "PARAMETRO_PGPM",
                columns: new[] { "CLAVE", "ACTUALIZADO_EN", "DESCRIPCION", "VALOR" },
                values: new object[,]
                {
                    { "AVANCE.BASE_PLAN", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Con qué se arma el % programado de cada semana: ACTIVIDADES o DIAS del plan de Oracle. Pendiente de confirmar.", "ACTIVIDADES" },
                    { "AVANCE.CLASES_ORDEN", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Clases de orden (separadas por coma) del programa de reparación que cuentan para Avance.", "ZC03" },
                    { "DISPONIBILIDAD.CLASES_ORDEN", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Clases de orden que dejan equipos fuera de servicio, separadas por coma; * = todas. Pendiente: ¿cuentan las ZC03 del programa de reparación?", "*" },
                    { "DISPONIBILIDAD.CORRECTIVAS_SEGUN_AVISO", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "S = una correctiva con aviso solo deja el equipo fuera si el aviso indica parada, desde el inicio hasta el fin de la avería.", "S" },
                    { "DISPONIBILIDAD.INICIO_VENTANA", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Desde cuándo cuenta fuera de servicio: CREACION de la orden (respuesta del 01/10/2026) o INICIO_REAL.", "CREACION" },
                    { "JOB.DIAS_RECALCULO_DISPONIBILIDAD", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Días hacia atrás que se recalcula la disponibilidad en cada corrida.", "120" },
                    { "JOB.HORA_CORRIDA_DIARIA", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hora local (HH:mm) de la corrida diaria del job de extracción y recálculo.", "05:00" },
                    { "MONITOREO.HORAS_ALERTA", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Horas sin una corrida exitosa a partir de las cuales se alerta (sección 11).", "24" },
                    { "SAP.ESTADOS_CERRADOS", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Estados de orden que la cierran (fin de la indisponibilidad).", "Cerrada" },
                    { "SAP.ESTADOS_EXCLUIDOS", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Estados de orden que no cuentan ni para disponibilidad ni para avance.", "Anulada" },
                    { "SAP.FECHA_CARGA_INICIAL", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fecha (yyyy-MM-dd) desde la que se extrae de SAP la primera vez: salida en vivo de SAP PM.", "2026-06-01" },
                    { "SAP.TIPO_POR_CLASE", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Clase de orden SAP -> tipo de horas (CORRECTIVA / PREVENTIVO / PLANIFICADA). Pendiente de confirmar con el ABAP (clases y ILART reales).", "ZC01=CORRECTIVA,ZC02=PREVENTIVO,ZC03=PLANIFICADA" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ALERTA_TIPO_FECHA_PGPM",
                table: "ALERTA_ENVIADA_PGPM",
                columns: new[] { "TIPO", "FECHA" });

            migrationBuilder.CreateIndex(
                name: "IX_CALC_DISP_DIARIA_FEC_PGPM",
                table: "CALC_DISP_DIARIA_PGPM",
                columns: new[] { "REPORTE_ID", "FECHA" });

            migrationBuilder.CreateIndex(
                name: "IX_LOG_CORRIDA_PGPM",
                table: "LOG_EXTRACCION_PGPM",
                column: "CORRIDA_ID");

            migrationBuilder.CreateIndex(
                name: "IX_LOG_FUENTE_ESTADO_PGPM",
                table: "LOG_EXTRACCION_PGPM",
                columns: new[] { "FUENTE", "ESTADO", "INICIO" });

            migrationBuilder.CreateIndex(
                name: "IX_STG_AVISO_ORDEN_PGPM",
                table: "STG_SAP_AVISO_PGPM",
                column: "ORDEN_SAP_ID");

            migrationBuilder.CreateIndex(
                name: "IX_STG_EQUIPO_LEGADO_PGPM",
                table: "STG_SAP_EQUIPO_PGPM",
                column: "CODIGO_LEGADO");

            migrationBuilder.CreateIndex(
                name: "IX_STG_HIST_ORDEN_PGPM",
                table: "STG_SAP_HIST_ESTADO_PGPM",
                columns: new[] { "ORDEN_SAP_ID", "FECHA_HORA_CAMBIO" });

            migrationBuilder.CreateIndex(
                name: "IX_STG_OPERACION_CONF_PGPM",
                table: "STG_SAP_OPERACION_PGPM",
                column: "FECHA_ULTIMA_CONF");

            migrationBuilder.CreateIndex(
                name: "IX_STG_ORDEN_CLASE_PGPM",
                table: "STG_SAP_ORDEN_PGPM",
                column: "CLASE_ORDEN");

            migrationBuilder.CreateIndex(
                name: "IX_STG_ORDEN_EQUIPO_PGPM",
                table: "STG_SAP_ORDEN_PGPM",
                column: "EQUIPO_SAP_ID");

            migrationBuilder.CreateIndex(
                name: "IX_STG_ORDEN_FECHAS_PGPM",
                table: "STG_SAP_ORDEN_PGPM",
                columns: new[] { "FECHA_CREACION", "FECHA_CIERRE" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ALERTA_ENVIADA_PGPM");

            migrationBuilder.DropTable(
                name: "CALC_AVANCE_SEMANAL_PGPM");

            migrationBuilder.DropTable(
                name: "CALC_DISP_DIARIA_PGPM");

            migrationBuilder.DropTable(
                name: "CALC_DISP_SEMANAL_PGPM");

            migrationBuilder.DropTable(
                name: "CALC_EQUIPO_SIN_CLASIF_PGPM");

            migrationBuilder.DropTable(
                name: "LOG_EXTRACCION_PGPM");

            migrationBuilder.DropTable(
                name: "PARAMETRO_PGPM");

            migrationBuilder.DropTable(
                name: "STG_SAP_AVISO_PGPM");

            migrationBuilder.DropTable(
                name: "STG_SAP_HIST_ESTADO_PGPM");

            migrationBuilder.DropTable(
                name: "STG_SAP_OPERACION_PGPM");

            migrationBuilder.DropTable(
                name: "STG_SAP_ORDEN_PGPM");

            migrationBuilder.DropTable(
                name: "STG_SAP_EQUIPO_PGPM");
        }
    }
}
