using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMfaEmpleado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MfaBloqueadoHasta",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MfaHabilitado",
                table: "Usuarios",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MfaIntentosFallidos",
                table: "Usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MfaSecret",
                table: "Usuarios",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CodigosRecuperacionMfa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpleadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodigoHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Usado = table.Column<bool>(type: "boolean", nullable: false),
                    FechaUso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodigosRecuperacionMfa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodigosRecuperacionMfa_Usuarios_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "MfaBloqueadoHasta", "MfaHabilitado", "MfaIntentosFallidos", "MfaSecret" },
                values: new object[] { null, false, 0, null });

            migrationBuilder.CreateIndex(
                name: "IX_CodigosRecuperacionMfa_EmpleadoId",
                table: "CodigosRecuperacionMfa",
                column: "EmpleadoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodigosRecuperacionMfa");

            migrationBuilder.DropColumn(
                name: "MfaBloqueadoHasta",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "MfaHabilitado",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "MfaIntentosFallidos",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "MfaSecret",
                table: "Usuarios");
        }
    }
}
