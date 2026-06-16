using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CambiarInscripcionDeClaseAHorario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InscripcionesClase_Clases_ClaseId",
                table: "InscripcionesClase");

            migrationBuilder.DropIndex(
                name: "IX_InscripcionesClase_ClaseId",
                table: "InscripcionesClase");

            migrationBuilder.AddColumn<Guid>(
                name: "HorarioClaseId",
                table: "InscripcionesClase",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "InscripcionesClase" i
                SET "HorarioClaseId" = (
                    SELECT h."Id"
                    FROM "HorariosClase" h
                    WHERE h."ClaseId" = i."ClaseId"
                    ORDER BY h."DiaSemana", h."HoraInicio"
                    LIMIT 1
                )
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "InscripcionesClase"
                        WHERE "HorarioClaseId" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'No se puede migrar InscripcionesClase: existen inscripciones sin horario asociado a su clase.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "HorarioClaseId",
                table: "InscripcionesClase",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "ClaseId",
                table: "InscripcionesClase");

            migrationBuilder.CreateIndex(
                name: "IX_InscripcionesClase_HorarioClaseId",
                table: "InscripcionesClase",
                column: "HorarioClaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_InscripcionesClase_HorariosClase_HorarioClaseId",
                table: "InscripcionesClase",
                column: "HorarioClaseId",
                principalTable: "HorariosClase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InscripcionesClase_HorariosClase_HorarioClaseId",
                table: "InscripcionesClase");

            migrationBuilder.DropIndex(
                name: "IX_InscripcionesClase_HorarioClaseId",
                table: "InscripcionesClase");

            migrationBuilder.AddColumn<Guid>(
                name: "ClaseId",
                table: "InscripcionesClase",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "InscripcionesClase" i
                SET "ClaseId" = h."ClaseId"
                FROM "HorariosClase" h
                WHERE h."Id" = i."HorarioClaseId"
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "InscripcionesClase"
                        WHERE "ClaseId" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'No se puede revertir InscripcionesClase: existen inscripciones sin clase asociada a su horario.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClaseId",
                table: "InscripcionesClase",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "HorarioClaseId",
                table: "InscripcionesClase");

            migrationBuilder.CreateIndex(
                name: "IX_InscripcionesClase_ClaseId",
                table: "InscripcionesClase",
                column: "ClaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_InscripcionesClase_Clases_ClaseId",
                table: "InscripcionesClase",
                column: "ClaseId",
                principalTable: "Clases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
