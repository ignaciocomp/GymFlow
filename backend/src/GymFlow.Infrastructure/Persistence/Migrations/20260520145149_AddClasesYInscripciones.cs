using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClasesYInscripciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CapacidadMaxima = table.Column<int>(type: "integer", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "integer", nullable: false),
                    Instructor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UnidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstaActivo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clases_Unidades_UnidadId",
                        column: x => x.UnidadId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InscripcionesClase",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SocioId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaInscripcion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EstaActiva = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InscripcionesClase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InscripcionesClase_Clases_ClaseId",
                        column: x => x.ClaseId,
                        principalTable: "Clases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InscripcionesClase_Usuarios_SocioId",
                        column: x => x.SocioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Modulo", "Operacion" },
                values: new object[,]
                {
                    { new Guid("1a3c6f4b-04a1-5fab-f185-742b93489621"), "Clases", "Escritura" },
                    { new Guid("7259fbb5-4c0b-1df4-e552-20fb3c1c9e94"), "Clases", "Eliminacion" },
                    { new Guid("c8aa70e1-cd1a-b8d7-e3d8-5fb9c20eb0c8"), "Clases", "Lectura" },
                    { new Guid("d44e2085-d440-af40-9121-82c745f8e7c8"), "Clases", "Modificacion" }
                });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermisoId", "RolId" },
                values: new object[,]
                {
                    { new Guid("1a3c6f4b-04a1-5fab-f185-742b93489621"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("7259fbb5-4c0b-1df4-e552-20fb3c1c9e94"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("c8aa70e1-cd1a-b8d7-e3d8-5fb9c20eb0c8"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("d44e2085-d440-af40-9121-82c745f8e7c8"), new Guid("11111111-1111-1111-1111-111111111111") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clases_UnidadId",
                table: "Clases",
                column: "UnidadId");

            migrationBuilder.CreateIndex(
                name: "IX_InscripcionesClase_ClaseId",
                table: "InscripcionesClase",
                column: "ClaseId");

            migrationBuilder.CreateIndex(
                name: "IX_InscripcionesClase_SocioId",
                table: "InscripcionesClase",
                column: "SocioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InscripcionesClase");

            migrationBuilder.DropTable(
                name: "Clases");

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("1a3c6f4b-04a1-5fab-f185-742b93489621"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("7259fbb5-4c0b-1df4-e552-20fb3c1c9e94"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("c8aa70e1-cd1a-b8d7-e3d8-5fb9c20eb0c8"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("d44e2085-d440-af40-9121-82c745f8e7c8"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("1a3c6f4b-04a1-5fab-f185-742b93489621"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("7259fbb5-4c0b-1df4-e552-20fb3c1c9e94"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("c8aa70e1-cd1a-b8d7-e3d8-5fb9c20eb0c8"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("d44e2085-d440-af40-9121-82c745f8e7c8"));
        }
    }
}
