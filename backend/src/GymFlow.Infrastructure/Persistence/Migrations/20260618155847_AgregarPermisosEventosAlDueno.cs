using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPermisosEventosAlDueno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermisoId", "RolId" },
                values: new object[,]
                {
                    { new Guid("8bc5a423-e1a7-ebcc-7d1c-d357b366fb67"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("9f769d0b-11d1-7fa2-c055-115036c5740e"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("ac5cbe75-f885-9361-97ba-3cc7bab79897"), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("e7b29507-cf1a-313e-812c-62d8eec15a1a"), new Guid("44444444-4444-4444-4444-444444444444") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("8bc5a423-e1a7-ebcc-7d1c-d357b366fb67"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("9f769d0b-11d1-7fa2-c055-115036c5740e"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("ac5cbe75-f885-9361-97ba-3cc7bab79897"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("e7b29507-cf1a-313e-812c-62d8eec15a1a"), new Guid("44444444-4444-4444-4444-444444444444") });
        }
    }
}
