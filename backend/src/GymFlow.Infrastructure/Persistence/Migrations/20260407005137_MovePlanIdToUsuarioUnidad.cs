using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MovePlanIdToUsuarioUnidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Planes_PlanId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_PlanId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "Usuarios");

            migrationBuilder.AddColumn<Guid>(
                name: "PlanId",
                table: "UsuarioUnidades",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioUnidades_PlanId",
                table: "UsuarioUnidades",
                column: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioUnidades_Planes_PlanId",
                table: "UsuarioUnidades",
                column: "PlanId",
                principalTable: "Planes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioUnidades_Planes_PlanId",
                table: "UsuarioUnidades");

            migrationBuilder.DropIndex(
                name: "IX_UsuarioUnidades_PlanId",
                table: "UsuarioUnidades");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "UsuarioUnidades");

            migrationBuilder.AddColumn<Guid>(
                name: "PlanId",
                table: "Usuarios",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_PlanId",
                table: "Usuarios",
                column: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Planes_PlanId",
                table: "Usuarios",
                column: "PlanId",
                principalTable: "Planes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
