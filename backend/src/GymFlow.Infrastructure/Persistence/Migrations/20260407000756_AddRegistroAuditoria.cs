using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistroAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosAuditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TipoAccion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntidadAfectada = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntidadId = table.Column<Guid>(type: "uuid", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DetallesCambios = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FechaHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_EntidadId",
                table: "RegistrosAuditoria",
                column: "EntidadId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_FechaHora",
                table: "RegistrosAuditoria",
                column: "FechaHora",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_UsuarioId",
                table: "RegistrosAuditoria",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosAuditoria");
        }
    }
}
