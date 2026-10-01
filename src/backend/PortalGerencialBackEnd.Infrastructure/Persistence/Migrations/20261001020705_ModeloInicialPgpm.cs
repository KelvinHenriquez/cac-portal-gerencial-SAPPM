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
                name: "CALC_AVANCE_SEMANAL_PGPM",
                columns: table => new
                {
                    FAMILIA_CODIGO = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: false),
                    ANIO_PROGRAMA = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    SEMANA = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    FECHA_INICIO_SEMANA = table.Column<DateTime>(type: "DATE", nullable: false),
                    FECHA_FIN_SEMANA = table.Column<DateTime>(type: "DATE", nullable: false),
                    PROG_PCT = table.Column<decimal>(type: "DECIMAL(5,2)", precision: 5, scale: 2, nullable: false),
                    REAL_PCT = table.Column<decimal>(type: "DECIMAL(5,2)", precision: 5, scale: 2, nullable: false),
                    PEND_PCT = table.Column<decimal>(type: "DECIMAL(6,2)", precision: 6, scale: 2, nullable: false),
                    TOTAL_ORDENES = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ORDENES_CERRADAS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    HH_PLAN = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    HH_REAL = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    CORRIDA_ID = table.Column<string>(type: "VARCHAR2(36)", unicode: false, maxLength: 36, nullable: false),
                    GENERADO_EN = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CALC_AVANCE_SEMANAL_PGPM", x => new { x.FAMILIA_CODIGO, x.ANIO_PROGRAMA, x.SEMANA });
                });

            migrationBuilder.CreateTable(
                name: "CALC_DISP_DIARIA_PGPM",
                columns: table => new
                {
                    TIPO_REPORTE = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: false),
                    FAMILIA_CODIGO = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: false),
                    FECHA = table.Column<DateTime>(type: "DATE", nullable: false),
                    CANT_EQUIPO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EQ_REQUERIDO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EQ_FUERA_SERVICIO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EQ_DISPONIBLE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DISP_REAL_PCT = table.Column<decimal>(type: "DECIMAL(5,2)", precision: 5, scale: 2, nullable: false),
                    DISP_POTENCIAL_PCT = table.Column<decimal>(type: "DECIMAL(5,2)", precision: 5, scale: 2, nullable: false),
                    CORRIDA_ID = table.Column<string>(type: "VARCHAR2(36)", unicode: false, maxLength: 36, nullable: false),
                    GENERADO_EN = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CALC_DISP_DIARIA_PGPM", x => new { x.TIPO_REPORTE, x.FAMILIA_CODIGO, x.FECHA });
                });

            migrationBuilder.CreateTable(
                name: "CALC_DISP_SEMANAL_PGPM",
                columns: table => new
                {
                    TIPO_REPORTE = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: false),
                    FAMILIA_CODIGO = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: false),
                    ANIO_ISO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    SEMANA_ISO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    FECHA_INICIO_SEMANA = table.Column<DateTime>(type: "DATE", nullable: false),
                    FECHA_FIN_SEMANA = table.Column<DateTime>(type: "DATE", nullable: false),
                    DIAS_CALCULADOS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CANT_EQUIPO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EQ_REQUERIDO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EQ_DISPONIBLE_PROM = table.Column<decimal>(type: "DECIMAL(10,2)", precision: 10, scale: 2, nullable: false),
                    EQ_DISPONIBLE_MIN = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DISP_REAL_PCT = table.Column<decimal>(type: "DECIMAL(5,2)", precision: 5, scale: 2, nullable: false),
                    DISP_REAL_MIN_PCT = table.Column<decimal>(type: "DECIMAL(5,2)", precision: 5, scale: 2, nullable: false),
                    DISP_POTENCIAL_PCT = table.Column<decimal>(type: "DECIMAL(5,2)", precision: 5, scale: 2, nullable: false),
                    CORRIDA_ID = table.Column<string>(type: "VARCHAR2(36)", unicode: false, maxLength: 36, nullable: false),
                    GENERADO_EN = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CALC_DISP_SEMANAL_PGPM", x => new { x.TIPO_REPORTE, x.FAMILIA_CODIGO, x.ANIO_ISO, x.SEMANA_ISO });
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
                    DETECTADO_EN = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CALC_EQ_SIN_CLASIF_PGPM", x => x.EQUIPO_SAP_ID);
                });

            migrationBuilder.CreateTable(
                name: "LOG_EXTRACCION_PGPM",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
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
                    ACTUALIZADO_EN = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PARAMETRO_PGPM", x => x.CLAVE);
                });

            migrationBuilder.CreateTable(
                name: "STG_SAP_EQUIPO_PGPM",
                columns: table => new
                {
                    EQUIPO_SAP_ID = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: false),
                    CODIGO_LEGADO = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: true),
                    DESCRIPCION = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: true),
                    PUESTO_TRABAJO = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: true),
                    ACTUALIZADO_EN = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSTIMESTAMP")
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
                    ACTUALIZADO_EN = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSTIMESTAMP")
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
                    CLASE_ORDEN = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: false),
                    EQUIPO_SAP_ID = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: true),
                    DESCRIPCION = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: true),
                    PUESTO_TRABAJO = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: true),
                    ESTADO_ORDEN = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: false),
                    FECHA_CREACION = table.Column<DateTime>(type: "DATE", nullable: true),
                    FECHA_INICIO_PLAN = table.Column<DateTime>(type: "DATE", nullable: true),
                    FECHA_FIN_PLAN = table.Column<DateTime>(type: "DATE", nullable: true),
                    FECHA_INICIO_REAL = table.Column<DateTime>(type: "DATE", nullable: true),
                    FECHA_FIN_REAL = table.Column<DateTime>(type: "DATE", nullable: true),
                    ACTUALIZADO_EN = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSTIMESTAMP")
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
                name: "STG_SAP_OPERACION_HORAS_PGPM",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ORDEN_SAP_ID = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    OPERACION = table.Column<string>(type: "VARCHAR2(10)", unicode: false, maxLength: 10, nullable: false),
                    TRABAJO_PLAN_HH = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    TRABAJO_REAL_HH = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    ACTUALIZADO_EN = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STG_OPER_HORAS_PGPM", x => x.ID);
                    table.ForeignKey(
                        name: "FK_STG_OPER_ORDEN_PGPM",
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
                    { "AVANCE.PROGRAMA_VIGENTE", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Programa de reparación que se abre por defecto en Avance de Reparación.", "2026" },
                    { "JOB.DIAS_RECALCULO_DISPONIBILIDAD", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Días hacia atrás que se recalcula la disponibilidad diaria en cada corrida.", "120" },
                    { "JOB.HORA_CORRIDA_DIARIA", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hora local (HH:mm) de la corrida diaria del job de extracción y recálculo.", "05:00" },
                    { "MONITOREO.HORAS_ALERTA", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Horas sin una corrida exitosa a partir de las cuales se alerta (sección 11).", "24" },
                    { "SAP.CLASES_ORDEN_AVANCE", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Clases de orden (separadas por coma) que cuentan para Avance de Reparación (sección 4.2).", "ZC03" },
                    { "SAP.ESTADOS_CERRADOS", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Estados de orden que cuentan como orden terminada para el avance de reparación.", "Cerrado tecnicamente,Cerrado" },
                    { "SAP.ESTADOS_FUERA_SERVICIO", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Estados de orden que dejan al equipo fuera de servicio (pendiente de confirmar con taller, sección 6.1).", "Liberado,En Proceso" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CALC_AVANCE_PROG_PGPM",
                table: "CALC_AVANCE_SEMANAL_PGPM",
                column: "ANIO_PROGRAMA");

            migrationBuilder.CreateIndex(
                name: "IX_CALC_DISP_DIARIA_FEC_PGPM",
                table: "CALC_DISP_DIARIA_PGPM",
                columns: new[] { "TIPO_REPORTE", "FECHA" });

            migrationBuilder.CreateIndex(
                name: "IX_CALC_DISP_SEM_FEC_PGPM",
                table: "CALC_DISP_SEMANAL_PGPM",
                columns: new[] { "TIPO_REPORTE", "FECHA_INICIO_SEMANA" });

            migrationBuilder.CreateIndex(
                name: "IX_LOG_CORRIDA_PGPM",
                table: "LOG_EXTRACCION_PGPM",
                column: "CORRIDA_ID");

            migrationBuilder.CreateIndex(
                name: "IX_LOG_FUENTE_ESTADO_PGPM",
                table: "LOG_EXTRACCION_PGPM",
                columns: new[] { "FUENTE", "ESTADO", "INICIO" });

            migrationBuilder.CreateIndex(
                name: "IX_STG_EQUIPO_LEGADO_PGPM",
                table: "STG_SAP_EQUIPO_PGPM",
                column: "CODIGO_LEGADO");

            migrationBuilder.CreateIndex(
                name: "IX_STG_HIST_ORDEN_PGPM",
                table: "STG_SAP_HIST_ESTADO_PGPM",
                columns: new[] { "ORDEN_SAP_ID", "FECHA_HORA_CAMBIO" });

            migrationBuilder.CreateIndex(
                name: "UQ_STG_OPER_HORAS_PGPM",
                table: "STG_SAP_OPERACION_HORAS_PGPM",
                columns: new[] { "ORDEN_SAP_ID", "OPERACION" },
                unique: true);

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
                columns: new[] { "FECHA_INICIO_REAL", "FECHA_FIN_REAL" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "STG_SAP_HIST_ESTADO_PGPM");

            migrationBuilder.DropTable(
                name: "STG_SAP_OPERACION_HORAS_PGPM");

            migrationBuilder.DropTable(
                name: "STG_SAP_ORDEN_PGPM");

            migrationBuilder.DropTable(
                name: "STG_SAP_EQUIPO_PGPM");
        }
    }
}
