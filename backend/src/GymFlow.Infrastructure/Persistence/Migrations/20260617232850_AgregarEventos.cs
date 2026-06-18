using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEventos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Eventos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UnidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstaActivo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eventos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Eventos_Unidades_UnidadId",
                        column: x => x.UnidadId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Modulo", "Operacion" },
                values: new object[,]
                {
                    { new Guid("8bc5a423-e1a7-ebcc-7d1c-d357b366fb67"), "Eventos", "Lectura" },
                    { new Guid("9f769d0b-11d1-7fa2-c055-115036c5740e"), "Eventos", "Escritura" },
                    { new Guid("ac5cbe75-f885-9361-97ba-3cc7bab79897"), "Eventos", "Modificacion" },
                    { new Guid("e7b29507-cf1a-313e-812c-62d8eec15a1a"), "Eventos", "Eliminacion" }
                });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermisoId", "RolId" },
                values: new object[,]
                {
                    { new Guid("8bc5a423-e1a7-ebcc-7d1c-d357b366fb67"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("9f769d0b-11d1-7fa2-c055-115036c5740e"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("ac5cbe75-f885-9361-97ba-3cc7bab79897"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("e7b29507-cf1a-313e-812c-62d8eec15a1a"), new Guid("11111111-1111-1111-1111-111111111111") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_Fecha",
                table: "Eventos",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_UnidadId",
                table: "Eventos",
                column: "UnidadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Eventos");

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("8bc5a423-e1a7-ebcc-7d1c-d357b366fb67"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("9f769d0b-11d1-7fa2-c055-115036c5740e"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("ac5cbe75-f885-9361-97ba-3cc7bab79897"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("e7b29507-cf1a-313e-812c-62d8eec15a1a"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("8bc5a423-e1a7-ebcc-7d1c-d357b366fb67"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("9f769d0b-11d1-7fa2-c055-115036c5740e"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("ac5cbe75-f885-9361-97ba-3cc7bab79897"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("e7b29507-cf1a-313e-812c-62d8eec15a1a"));
        }
    }
}
