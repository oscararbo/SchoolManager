using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Back.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiSchoolSuperusuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cuentas_Correo",
                table: "Cuentas");

            migrationBuilder.AddColumn<int>(
                name: "ColegioId",
                table: "Cursos",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ColegioId",
                table: "Cuentas",
                type: "integer",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Colegios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colegios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cursos_ColegioId",
                table: "Cursos",
                column: "ColegioId");

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_ColegioId_Correo",
                table: "Cuentas",
                columns: new[] { "ColegioId", "Correo" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Colegios_Slug",
                table: "Colegios",
                column: "Slug",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.Sql(
                """
                INSERT INTO "Colegios" ("Id", "Nombre", "Slug", "LogoUrl", "IsDeleted")
                SELECT 1, 'Colegio Principal', 'default', NULL, FALSE
                WHERE NOT EXISTS (SELECT 1 FROM "Colegios" WHERE "Id" = 1);
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Cuentas"
                SET "ColegioId" = 1
                WHERE "ColegioId" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Cursos"
                SET "ColegioId" = 1
                WHERE "ColegioId" IS NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Cuentas_Colegios_ColegioId",
                table: "Cuentas",
                column: "ColegioId",
                principalTable: "Colegios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cursos_Colegios_ColegioId",
                table: "Cursos",
                column: "ColegioId",
                principalTable: "Colegios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cuentas_Colegios_ColegioId",
                table: "Cuentas");

            migrationBuilder.DropForeignKey(
                name: "FK_Cursos_Colegios_ColegioId",
                table: "Cursos");

            migrationBuilder.DropTable(
                name: "Colegios");

            migrationBuilder.DropIndex(
                name: "IX_Cursos_ColegioId",
                table: "Cursos");

            migrationBuilder.DropIndex(
                name: "IX_Cuentas_ColegioId_Correo",
                table: "Cuentas");

            migrationBuilder.DropColumn(
                name: "ColegioId",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "ColegioId",
                table: "Cuentas");

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_Correo",
                table: "Cuentas",
                column: "Correo",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }
    }
}
