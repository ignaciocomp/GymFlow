using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpleadosYPermisosEmpleados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Usuarios",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Modulo", "Operacion" },
                values: new object[,]
                {
                    { new Guid("49569ee3-9b6b-e594-865a-6dd30f40aa88"), "Empleados", "Eliminacion" },
                    { new Guid("5171a06c-b17d-4f3d-3d99-8aa4d8321c38"), "Empleados", "Modificacion" },
                    { new Guid("5301567d-6106-30f0-f64c-726bfe81634c"), "Empleados", "Escritura" },
                    { new Guid("c75a7329-9ecf-23de-168b-df43e5d82268"), "Empleados", "Lectura" }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Apellido", "Correo", "EstaActivo", "FechaCreacion", "Nombre", "PasswordHash", "RolId", "TipoUsuario" },
                values: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), "Inicial", "admin@gymflow.com", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Admin", "$2a$11$8TnD1uScCjtswRRfjtIMDufn8npEr3r1lKxd/aJ6LCv9wFtEPjvXS", new Guid("11111111-1111-1111-1111-111111111111"), "Empleado" });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermisoId", "RolId" },
                values: new object[,]
                {
                    { new Guid("49569ee3-9b6b-e594-865a-6dd30f40aa88"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("5171a06c-b17d-4f3d-3d99-8aa4d8321c38"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("5301567d-6106-30f0-f64c-726bfe81634c"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("c75a7329-9ecf-23de-168b-df43e5d82268"), new Guid("11111111-1111-1111-1111-111111111111") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("49569ee3-9b6b-e594-865a-6dd30f40aa88"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("5171a06c-b17d-4f3d-3d99-8aa4d8321c38"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("5301567d-6106-30f0-f64c-726bfe81634c"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("c75a7329-9ecf-23de-168b-df43e5d82268"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("49569ee3-9b6b-e594-865a-6dd30f40aa88"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("5171a06c-b17d-4f3d-3d99-8aa4d8321c38"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("5301567d-6106-30f0-f64c-726bfe81634c"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("c75a7329-9ecf-23de-168b-df43e5d82268"));

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Usuarios",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
