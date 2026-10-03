using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BasePorcentajeDisponibilidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PARAMETRO_PGPM",
                columns: new[] { "CLAVE", "ACTUALIZADO_EN", "DESCRIPCION", "VALOR" },
                values: new object[] { "DISPONIBILIDAD.BASE_PORCENTAJE", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Base del % de disponibilidad: REQUERIDOS (disponibles / requeridos, topado en 100) o EQUIPOS (disponibles / equipos de la asignación, como el PL/SQL de Oracle PR_DISPONIBILIDAD_EQUIPOS).", "REQUERIDOS" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "DISPONIBILIDAD.BASE_PORCENTAJE");
        }
    }
}
