using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AvancePesoActividad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PARAMETRO_PGPM",
                columns: new[] { "CLAVE", "ACTUALIZADO_EN", "ACTUALIZADO_POR", "DESCRIPCION", "VALOR" },
                values: new object[] { "AVANCE.PESO_ACTIVIDAD", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Avance por actividades: cuánto vale cada actividad terminada. CANTIDAD = 1 cada una, como Oracle; HORAS_PLAN = su trabajo planificado; DIAS = duración entre sus fechas plan (en SAP suelen ser iguales: daría 0) (07/10/2026).", "CANTIDAD" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "AVANCE.PESO_ACTIVIDAD");
        }
    }
}
