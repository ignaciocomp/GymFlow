using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Modulo", "Operacion" },
                values: new object[,]
                {
                    { new Guid("44f223d1-38f4-0362-28b5-fc33e387e0f4"), "Dashboard", "Escritura" },
                    { new Guid("48b8efd0-9432-354d-c4fb-e2e907481c12"), "Dashboard", "Eliminacion" },
                    { new Guid("8484d71e-a7cb-4c39-5457-f4a28b869b95"), "Dashboard", "Lectura" },
                    { new Guid("c0b61374-3e06-dbd2-8618-d283d71a16c7"), "Dashboard", "Modificacion" }
                });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermisoId", "RolId" },
                values: new object[,]
                {
                    { new Guid("44f223d1-38f4-0362-28b5-fc33e387e0f4"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("48b8efd0-9432-354d-c4fb-e2e907481c12"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("8484d71e-a7cb-4c39-5457-f4a28b869b95"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("c0b61374-3e06-dbd2-8618-d283d71a16c7"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("8484d71e-a7cb-4c39-5457-f4a28b869b95"), new Guid("44444444-4444-4444-4444-444444444444") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("44f223d1-38f4-0362-28b5-fc33e387e0f4"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("48b8efd0-9432-354d-c4fb-e2e907481c12"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("8484d71e-a7cb-4c39-5457-f4a28b869b95"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("c0b61374-3e06-dbd2-8618-d283d71a16c7"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("8484d71e-a7cb-4c39-5457-f4a28b869b95"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("44f223d1-38f4-0362-28b5-fc33e387e0f4"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("48b8efd0-9432-354d-c4fb-e2e907481c12"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("8484d71e-a7cb-4c39-5457-f4a28b869b95"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("c0b61374-3e06-dbd2-8618-d283d71a16c7"));
        }
    }
}
