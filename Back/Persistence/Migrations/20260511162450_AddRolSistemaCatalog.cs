using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Back.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRolSistemaCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RolSistemaId",
                table: "Cuentas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RolesSistema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolesSistema", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "RolesSistema",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "superusuario" },
                    { 2, "admin" },
                    { 3, "profesor" },
                    { 4, "alumno" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_RolSistemaId",
                table: "Cuentas",
                column: "RolSistemaId");

            migrationBuilder.CreateIndex(
                name: "IX_RolesSistema_Nombre",
                table: "RolesSistema",
                column: "Nombre",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cuentas_RolesSistema_RolSistemaId",
                table: "Cuentas",
                column: "RolSistemaId",
                principalTable: "RolesSistema",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cuentas_RolesSistema_RolSistemaId",
                table: "Cuentas");

            migrationBuilder.DropTable(
                name: "RolesSistema");

            migrationBuilder.DropIndex(
                name: "IX_Cuentas_RolSistemaId",
                table: "Cuentas");

            migrationBuilder.DropColumn(
                name: "RolSistemaId",
                table: "Cuentas");
        }
    }
}
