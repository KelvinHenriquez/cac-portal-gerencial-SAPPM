using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResumenAvance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RESUMEN_AVANCE_FILA_PGPM",
                columns: table => new
                {
                    AREA = table.Column<string>(type: "VARCHAR2(10)", unicode: false, maxLength: 10, nullable: false),
                    REPORTE_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    FAMILIA_CODIGO = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: false),
                    GRUPO = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: false),
                    NOMBRE = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: false),
                    ORDEN = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ACTUALIZADO_POR = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: true),
                    ACTUALIZADO_EN = table.Column<DateTime>(type: "DATE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RESUMEN_AVANCE_FILA_PGPM", x => new { x.AREA, x.REPORTE_ID, x.FAMILIA_CODIGO });
                });

            migrationBuilder.CreateTable(
                name: "RESUMEN_AVANCE_NOTA_PGPM",
                columns: table => new
                {
                    AREA = table.Column<string>(type: "VARCHAR2(10)", unicode: false, maxLength: 10, nullable: false),
                    REPORTE_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    FAMILIA_CODIGO = table.Column<string>(type: "VARCHAR2(30)", unicode: false, maxLength: 30, nullable: false),
                    SEMANA_FIN = table.Column<DateTime>(type: "DATE", nullable: false),
                    TEXTO = table.Column<string>(type: "VARCHAR2(300)", unicode: false, maxLength: 300, nullable: false),
                    ACTUALIZADO_POR = table.Column<string>(type: "VARCHAR2(60)", unicode: false, maxLength: 60, nullable: true),
                    ACTUALIZADO_EN = table.Column<DateTime>(type: "DATE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RESUMEN_AVANCE_NOTA_PGPM", x => new { x.AREA, x.REPORTE_ID, x.FAMILIA_CODIGO, x.SEMANA_FIN });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RESUMEN_AVANCE_FILA_PGPM");

            migrationBuilder.DropTable(
                name: "RESUMEN_AVANCE_NOTA_PGPM");
        }
    }
}
