using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCuotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cuotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocioId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombrePlan = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaBaja = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cuotas_Planes_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Planes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cuotas_Unidades_UnidadId",
                        column: x => x.UnidadId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cuotas_Usuarios_SocioId",
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
                    { new Guid("47bf3159-8b32-a19a-0d37-af59a58a5f73"), "Cuotas", "Modificacion" },
                    { new Guid("637b6d39-ebbf-9e16-8d7e-396861bd62ed"), "Cuotas", "Escritura" },
                    { new Guid("ef6441ba-c16e-cc6b-45a5-9e6375feb16d"), "Cuotas", "Lectura" },
                    { new Guid("efd06b26-0147-c35f-9b0e-a7d6b57194e1"), "Cuotas", "Eliminacion" }
                });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermisoId", "RolId" },
                values: new object[,]
                {
                    { new Guid("47bf3159-8b32-a19a-0d37-af59a58a5f73"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("637b6d39-ebbf-9e16-8d7e-396861bd62ed"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("ef6441ba-c16e-cc6b-45a5-9e6375feb16d"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("efd06b26-0147-c35f-9b0e-a7d6b57194e1"), new Guid("11111111-1111-1111-1111-111111111111") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cuotas_PlanId",
                table: "Cuotas",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Cuotas_SocioId",
                table: "Cuotas",
                column: "SocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Cuotas_UnidadId",
                table: "Cuotas",
                column: "UnidadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cuotas");

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("47bf3159-8b32-a19a-0d37-af59a58a5f73"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("637b6d39-ebbf-9e16-8d7e-396861bd62ed"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("ef6441ba-c16e-cc6b-45a5-9e6375feb16d"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("efd06b26-0147-c35f-9b0e-a7d6b57194e1"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("47bf3159-8b32-a19a-0d37-af59a58a5f73"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("637b6d39-ebbf-9e16-8d7e-396861bd62ed"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("ef6441ba-c16e-cc6b-45a5-9e6375feb16d"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("efd06b26-0147-c35f-9b0e-a7d6b57194e1"));
        }
    }
}
