using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRolDueno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "EsSistema", "FechaCreacion", "Nombre" },
                values: new object[] { new Guid("44444444-4444-4444-4444-444444444444"), true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dueño" });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermisoId", "RolId" },
                values: new object[,]
                {
                    { new Guid("0e3ccb52-631e-ab01-1b03-19eb3e0c166c"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("1a3c6f4b-04a1-5fab-f185-742b93489621"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("3db14f2d-e631-5d1f-998e-918eef77a623"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("46b1b86f-d5a2-efd5-a14b-f6e6cef155f1"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("47bf3159-8b32-a19a-0d37-af59a58a5f73"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("49569ee3-9b6b-e594-865a-6dd30f40aa88"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("5171a06c-b17d-4f3d-3d99-8aa4d8321c38"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("52c1feb8-98a7-3caa-35f5-df0f93d2a453"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("5301567d-6106-30f0-f64c-726bfe81634c"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("637b6d39-ebbf-9e16-8d7e-396861bd62ed"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("7259fbb5-4c0b-1df4-e552-20fb3c1c9e94"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("85236a48-46f3-3411-c68b-48b7d0bde83e"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("a31cd786-255b-16ac-e980-b899ee3e377b"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("a84626b4-4e1f-d4df-62c1-bd5b66ed2da7"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("c2700c7b-8aab-aff5-ad24-07f27b77f43b"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("c75a7329-9ecf-23de-168b-df43e5d82268"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("c8aa70e1-cd1a-b8d7-e3d8-5fb9c20eb0c8"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("cbeec377-4e4e-a9ec-c7e1-992e1b5fc994"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("d44e2085-d440-af40-9121-82c745f8e7c8"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("ef6441ba-c16e-cc6b-45a5-9e6375feb16d"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("efd06b26-0147-c35f-9b0e-a7d6b57194e1"), new Guid("44444444-4444-4444-4444-444444444444") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("0e3ccb52-631e-ab01-1b03-19eb3e0c166c"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("1a3c6f4b-04a1-5fab-f185-742b93489621"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("3db14f2d-e631-5d1f-998e-918eef77a623"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("46b1b86f-d5a2-efd5-a14b-f6e6cef155f1"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("47bf3159-8b32-a19a-0d37-af59a58a5f73"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("49569ee3-9b6b-e594-865a-6dd30f40aa88"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("5171a06c-b17d-4f3d-3d99-8aa4d8321c38"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("52c1feb8-98a7-3caa-35f5-df0f93d2a453"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("5301567d-6106-30f0-f64c-726bfe81634c"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("637b6d39-ebbf-9e16-8d7e-396861bd62ed"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("7259fbb5-4c0b-1df4-e552-20fb3c1c9e94"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("85236a48-46f3-3411-c68b-48b7d0bde83e"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a31cd786-255b-16ac-e980-b899ee3e377b"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a84626b4-4e1f-d4df-62c1-bd5b66ed2da7"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("c2700c7b-8aab-aff5-ad24-07f27b77f43b"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("c75a7329-9ecf-23de-168b-df43e5d82268"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("c8aa70e1-cd1a-b8d7-e3d8-5fb9c20eb0c8"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("cbeec377-4e4e-a9ec-c7e1-992e1b5fc994"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("d44e2085-d440-af40-9121-82c745f8e7c8"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("ef6441ba-c16e-cc6b-45a5-9e6375feb16d"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("efd06b26-0147-c35f-9b0e-a7d6b57194e1"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));
        }
    }
}
