using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UsuarioCreacionYHojaRuta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "USUARIO_CREACION",
                table: "STG_SAP_OPERACION_PGPM",
                type: "VARCHAR2(12)",
                unicode: false,
                maxLength: 12,
                nullable: true);

            migrationBuilder.InsertData(
                table: "PARAMETRO_PGPM",
                columns: new[] { "CLAVE", "ACTUALIZADO_EN", "DESCRIPCION", "VALOR" },
                values: new object[] { "SAP.USUARIOS_HOJA_RUTA", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Usuarios SAP (separados por coma) que crean las operaciones de la hoja de ruta (Real Programado); las creadas por otros usuarios son agregadas (Real No Programado). \"-\" = sin definir: se usa la marca que traiga SAP (respuesta del 02/10/2026).", "-" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "SAP.USUARIOS_HOJA_RUTA");

            migrationBuilder.DropColumn(
                name: "USUARIO_CREACION",
                table: "STG_SAP_OPERACION_PGPM");
        }
    }
}
