using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ParametroActualizadoPor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ACTUALIZADO_POR",
                table: "PARAMETRO_PGPM",
                type: "VARCHAR2(60)",
                unicode: false,
                maxLength: 60,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "AVANCE.BASE_PLAN",
                column: "ACTUALIZADO_POR",
                value: null);

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "AVANCE.CLASES_ORDEN",
                column: "ACTUALIZADO_POR",
                value: null);

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "DISPONIBILIDAD.BASE_PORCENTAJE",
                column: "ACTUALIZADO_POR",
                value: null);

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "DISPONIBILIDAD.CLASES_ORDEN",
                column: "ACTUALIZADO_POR",
                value: null);

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "DISPONIBILIDAD.CORRECTIVAS_SEGUN_AVISO",
                column: "ACTUALIZADO_POR",
                value: null);

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "DISPONIBILIDAD.INICIO_VENTANA",
                column: "ACTUALIZADO_POR",
                value: null);

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "JOB.DIAS_RECALCULO_DISPONIBILIDAD",
                column: "ACTUALIZADO_POR",
                value: null);

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "JOB.HORA_CORRIDA_DIARIA",
                column: "ACTUALIZADO_POR",
                value: null);

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "MONITOREO.HORAS_ALERTA",
                column: "ACTUALIZADO_POR",
                value: null);

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "SAP.ESTADOS_CERRADOS",
                column: "ACTUALIZADO_POR",
                value: null);

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "SAP.ESTADOS_EXCLUIDOS",
                column: "ACTUALIZADO_POR",
                value: null);

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "SAP.FECHA_CARGA_INICIAL",
                column: "ACTUALIZADO_POR",
                value: null);

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "SAP.TIPO_POR_CLASE",
                column: "ACTUALIZADO_POR",
                value: null);

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "SAP.USUARIOS_HOJA_RUTA",
                column: "ACTUALIZADO_POR",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ACTUALIZADO_POR",
                table: "PARAMETRO_PGPM");
        }
    }
}
