using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReglasDeOracleComoDefecto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "DISPONIBILIDAD.BASE_PORCENTAJE",
                columns: new[] { "DESCRIPCION", "VALOR" },
                values: new object[] { "Base del % de disponibilidad: EQUIPOS (disponibles / equipos de la asignación, como Oracle; decisión del 06/10/2026) o REQUERIDOS (disponibles / requeridos, topado en 100).", "EQUIPOS" });

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "DISPONIBILIDAD.CLASES_ORDEN",
                columns: new[] { "DESCRIPCION", "VALOR" },
                values: new object[] { "Clases de orden que dejan equipos fuera de servicio, separadas por coma; * = todas. Como Oracle, solo las correctivas (ZC01) (decisión del 06/10/2026; antes ZC01,ZC02).", "ZC01" });

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "DISPONIBILIDAD.CORRECTIVAS_SEGUN_AVISO",
                columns: new[] { "DESCRIPCION", "VALOR" },
                values: new object[] { "S = una correctiva con aviso solo deja el equipo fuera si el aviso indica parada, desde el inicio hasta el fin de la avería. N = como Oracle: toda correctiva abierta lo deja fuera (decisión del 06/10/2026).", "N" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "DISPONIBILIDAD.BASE_PORCENTAJE",
                columns: new[] { "DESCRIPCION", "VALOR" },
                values: new object[] { "Base del % de disponibilidad: REQUERIDOS (disponibles / requeridos, topado en 100) o EQUIPOS (disponibles / equipos de la asignación, como el PL/SQL de Oracle PR_DISPONIBILIDAD_EQUIPOS).", "REQUERIDOS" });

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "DISPONIBILIDAD.CLASES_ORDEN",
                columns: new[] { "DESCRIPCION", "VALOR" },
                values: new object[] { "Clases de orden que dejan equipos fuera de servicio, separadas por coma; * = todas. Cuentan correctivas y preventivas; las predictivas del programa de reparación (ZC03) no (respuesta del 02/10/2026).", "ZC01,ZC02" });

            migrationBuilder.UpdateData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "DISPONIBILIDAD.CORRECTIVAS_SEGUN_AVISO",
                columns: new[] { "DESCRIPCION", "VALOR" },
                values: new object[] { "S = una correctiva con aviso solo deja el equipo fuera si el aviso indica parada, desde el inicio hasta el fin de la avería.", "S" });
        }
    }
}
