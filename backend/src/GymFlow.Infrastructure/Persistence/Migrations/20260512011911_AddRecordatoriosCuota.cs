using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordatoriosCuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecordatoriosCuota",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CuotaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SocioId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoRecordatorio = table.Column<string>(type: "text", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Exitoso = table.Column<bool>(type: "boolean", nullable: false),
                    Error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordatoriosCuota", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecordatoriosCuota_Cuotas_CuotaId",
                        column: x => x.CuotaId,
                        principalTable: "Cuotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecordatoriosCuota_Usuarios_SocioId",
                        column: x => x.SocioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecordatoriosCuota_CuotaId_TipoRecordatorio_FechaEnvio",
                table: "RecordatoriosCuota",
                columns: new[] { "CuotaId", "TipoRecordatorio", "FechaEnvio" });

            migrationBuilder.CreateIndex(
                name: "IX_RecordatoriosCuota_SocioId",
                table: "RecordatoriosCuota",
                column: "SocioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecordatoriosCuota");
        }
    }
}
