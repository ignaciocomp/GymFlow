using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesYPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Usuarios");

            migrationBuilder.AddColumn<Guid>(
                name: "RolId",
                table: "Usuarios",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Modulo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Operacion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EsSistema = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolPermisos",
                columns: table => new
                {
                    RolId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermisoId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolPermisos", x => new { x.RolId, x.PermisoId });
                    table.ForeignKey(
                        name: "FK_RolPermisos_Permisos_PermisoId",
                        column: x => x.PermisoId,
                        principalTable: "Permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolPermisos_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Modulo", "Operacion" },
                values: new object[,]
                {
                    { new Guid("0e3ccb52-631e-ab01-1b03-19eb3e0c166c"), "Planes", "Lectura" },
                    { new Guid("3db14f2d-e631-5d1f-998e-918eef77a623"), "Planes", "Modificacion" },
                    { new Guid("46b1b86f-d5a2-efd5-a14b-f6e6cef155f1"), "Planes", "Escritura" },
                    { new Guid("4cd6600e-0c61-1f71-600c-fb4de03dc59d"), "Unidades", "Escritura" },
                    { new Guid("52c1feb8-98a7-3caa-35f5-df0f93d2a453"), "Socios", "Modificacion" },
                    { new Guid("67bd9ddb-74f0-a5da-ce55-4147ce6f5e64"), "Auditoria", "Modificacion" },
                    { new Guid("7cf53e27-0f70-c8a4-5047-d7468049499c"), "Auditoria", "Lectura" },
                    { new Guid("85236a48-46f3-3411-c68b-48b7d0bde83e"), "Socios", "Eliminacion" },
                    { new Guid("8e442f40-121a-eca9-23bc-54753ea7e6a7"), "Unidades", "Modificacion" },
                    { new Guid("a31cd786-255b-16ac-e980-b899ee3e377b"), "Socios", "Escritura" },
                    { new Guid("a84626b4-4e1f-d4df-62c1-bd5b66ed2da7"), "Unidades", "Lectura" },
                    { new Guid("a89d7550-6895-cb7c-c50c-2c3757eb0d7f"), "Auditoria", "Eliminacion" },
                    { new Guid("c2700c7b-8aab-aff5-ad24-07f27b77f43b"), "Socios", "Lectura" },
                    { new Guid("cbeec377-4e4e-a9ec-c7e1-992e1b5fc994"), "Planes", "Eliminacion" },
                    { new Guid("e0f92edd-c527-1b18-01a9-fec699383efb"), "Auditoria", "Escritura" },
                    { new Guid("e1906bb1-c65d-7ea9-5ddf-43679e4e4434"), "Unidades", "Eliminacion" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "EsSistema", "FechaCreacion", "Nombre" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Administrador" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Socio" }
                });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermisoId", "RolId" },
                values: new object[,]
                {
                    { new Guid("0e3ccb52-631e-ab01-1b03-19eb3e0c166c"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("3db14f2d-e631-5d1f-998e-918eef77a623"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("46b1b86f-d5a2-efd5-a14b-f6e6cef155f1"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("4cd6600e-0c61-1f71-600c-fb4de03dc59d"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("52c1feb8-98a7-3caa-35f5-df0f93d2a453"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("67bd9ddb-74f0-a5da-ce55-4147ce6f5e64"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("7cf53e27-0f70-c8a4-5047-d7468049499c"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("85236a48-46f3-3411-c68b-48b7d0bde83e"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("8e442f40-121a-eca9-23bc-54753ea7e6a7"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("a31cd786-255b-16ac-e980-b899ee3e377b"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("a84626b4-4e1f-d4df-62c1-bd5b66ed2da7"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("a89d7550-6895-cb7c-c50c-2c3757eb0d7f"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("c2700c7b-8aab-aff5-ad24-07f27b77f43b"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("cbeec377-4e4e-a9ec-c7e1-992e1b5fc994"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("e0f92edd-c527-1b18-01a9-fec699383efb"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("e1906bb1-c65d-7ea9-5ddf-43679e4e4434"), new Guid("11111111-1111-1111-1111-111111111111") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_RolId",
                table: "Usuarios",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_Modulo_Operacion",
                table: "Permisos",
                columns: new[] { "Modulo", "Operacion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Nombre",
                table: "Roles",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_PermisoId",
                table: "RolPermisos",
                column: "PermisoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Roles_RolId",
                table: "Usuarios",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Roles_RolId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "RolPermisos");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_RolId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "RolId",
                table: "Usuarios");

            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "Usuarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
