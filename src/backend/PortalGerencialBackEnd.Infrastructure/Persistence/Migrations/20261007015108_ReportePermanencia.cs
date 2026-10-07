using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReportePermanencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "INACTIVO",
                table: "STG_SAP_HIST_ESTADO_PGPM",
                type: "NUMBER(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GRUPO_PLANIFICACION",
                table: "STG_SAP_EQUIPO_PGPM",
                type: "VARCHAR2(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GRUPO_PLANIFICACION_DESC",
                table: "STG_SAP_EQUIPO_PGPM",
                type: "VARCHAR2(60)",
                unicode: false,
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TIPO_EQUIPO",
                table: "STG_SAP_EQUIPO_PGPM",
                type: "VARCHAR2(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TIPO_EQUIPO_DESC",
                table: "STG_SAP_EQUIPO_PGPM",
                type: "VARCHAR2(60)",
                unicode: false,
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UBICACION_TECNICA",
                table: "STG_SAP_EQUIPO_PGPM",
                type: "VARCHAR2(40)",
                unicode: false,
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UBICACION_TECNICA_DESC",
                table: "STG_SAP_EQUIPO_PGPM",
                type: "VARCHAR2(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CALC_PERMANENCIA_PGPM",
                columns: table => new
                {
                    ORDEN_SAP_ID = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    CLASE_ORDEN = table.Column<string>(type: "VARCHAR2(10)", unicode: false, maxLength: 10, nullable: false),
                    DESCRIPCION = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: true),
                    AVISO_SAP_ID = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: true),
                    EQUIPO_SAP_ID = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: false),
                    CLAVE_EQUIPO = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: false),
                    EQUIPO_DESCRIPCION = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: true),
                    AREA = table.Column<string>(type: "VARCHAR2(10)", unicode: false, maxLength: 10, nullable: false),
                    TIPO_EQUIPO = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    TIPO_EQUIPO_DESC = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: true),
                    INICIO = table.Column<DateTime>(type: "DATE", nullable: false),
                    FECHA_AVISO = table.Column<DateTime>(type: "DATE", nullable: true),
                    FECHA_CREADA = table.Column<DateTime>(type: "DATE", nullable: false),
                    FECHA_LIBERADA = table.Column<DateTime>(type: "DATE", nullable: true),
                    FECHA_EN_PROCESO = table.Column<DateTime>(type: "DATE", nullable: true),
                    FECHA_FINALIZADA = table.Column<DateTime>(type: "DATE", nullable: true),
                    FECHA_CIERRE_TECNICO = table.Column<DateTime>(type: "DATE", nullable: true),
                    TERMINADA = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    HORAS_RESPUESTA = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: true),
                    HORAS_LIBERACION = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: true),
                    HORAS_ESPERA_INICIO = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: true),
                    HORAS_EJECUCION = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: true),
                    HORAS_CIERRE_ADM = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: true),
                    HORAS_PERMANENCIA = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    HORAS_ESPERA_REPUESTOS = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    CORRIDA_ID = table.Column<string>(type: "VARCHAR2(36)", unicode: false, maxLength: 36, nullable: false),
                    GENERADO_EN = table.Column<DateTime>(type: "DATE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CALC_PERMANENCIA_PGPM", x => x.ORDEN_SAP_ID);
                });

            migrationBuilder.InsertData(
                table: "PARAMETRO_PGPM",
                columns: new[] { "CLAVE", "ACTUALIZADO_EN", "ACTUALIZADO_POR", "DESCRIPCION", "VALOR" },
                values: new object[,]
                {
                    { "PERMANENCIA.AREAS", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Áreas del reporte de permanencia por grupo de planificación (INGRP) del equipo en SAP: CODIGO=Nombre, separadas por coma.", "CDS=Centro de Servicios,IND=Industrial" },
                    { "PERMANENCIA.CLASES_ORDEN", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Clases de orden correctivas del reporte Tiempo de Permanencia: ZC01 (Centro de Servicios) y ZI01 (Industrial) (06/10/2026).", "ZC01,ZI01" },
                    { "PERMANENCIA.ESTADO_ESPERA", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Código del estado de usuario 'en espera de repuestos' (p. ej. E0004). \"-\" = todavía no existe en SAP: el tiempo de espera queda dentro de la ejecución.", "-" },
                    { "PERMANENCIA.ESTADOS_USUARIO", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Estados de usuario de las órdenes PM: ENPR = en proceso, FINL = finalizada (código del perfil de estados en JCDS).", "ENPR=E0002,FINL=E0003" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CALC_PERM_AREA_TIPO_PGPM",
                table: "CALC_PERMANENCIA_PGPM",
                columns: new[] { "AREA", "TIPO_EQUIPO" });

            migrationBuilder.CreateIndex(
                name: "IX_CALC_PERM_EQUIPO_PGPM",
                table: "CALC_PERMANENCIA_PGPM",
                column: "EQUIPO_SAP_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CALC_PERMANENCIA_PGPM");

            migrationBuilder.DeleteData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "PERMANENCIA.AREAS");

            migrationBuilder.DeleteData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "PERMANENCIA.CLASES_ORDEN");

            migrationBuilder.DeleteData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "PERMANENCIA.ESTADO_ESPERA");

            migrationBuilder.DeleteData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "PERMANENCIA.ESTADOS_USUARIO");

            migrationBuilder.DropColumn(
                name: "INACTIVO",
                table: "STG_SAP_HIST_ESTADO_PGPM");

            migrationBuilder.DropColumn(
                name: "GRUPO_PLANIFICACION",
                table: "STG_SAP_EQUIPO_PGPM");

            migrationBuilder.DropColumn(
                name: "GRUPO_PLANIFICACION_DESC",
                table: "STG_SAP_EQUIPO_PGPM");

            migrationBuilder.DropColumn(
                name: "TIPO_EQUIPO",
                table: "STG_SAP_EQUIPO_PGPM");

            migrationBuilder.DropColumn(
                name: "TIPO_EQUIPO_DESC",
                table: "STG_SAP_EQUIPO_PGPM");

            migrationBuilder.DropColumn(
                name: "UBICACION_TECNICA",
                table: "STG_SAP_EQUIPO_PGPM");

            migrationBuilder.DropColumn(
                name: "UBICACION_TECNICA_DESC",
                table: "STG_SAP_EQUIPO_PGPM");
        }
    }
}
