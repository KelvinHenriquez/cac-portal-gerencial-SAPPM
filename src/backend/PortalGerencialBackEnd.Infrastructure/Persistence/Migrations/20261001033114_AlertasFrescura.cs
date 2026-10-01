using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlertasFrescura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ALERTA_ENVIADA_PGPM",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
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

            migrationBuilder.CreateIndex(
                name: "IX_ALERTA_TIPO_FECHA_PGPM",
                table: "ALERTA_ENVIADA_PGPM",
                columns: new[] { "TIPO", "FECHA" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ALERTA_ENVIADA_PGPM");
        }
    }
}
