using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Back.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitCuentaPerfil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cuentas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Correo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Contrasena = table.Column<string>(type: "text", nullable: false),
                    Rol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuentas", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(
                name: "CuentaId",
                table: "Profesores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaId",
                table: "Estudiantes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaId",
                table: "Admins",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "Cuentas" ("Correo", "Contrasena", "Rol", "IsDeleted")
                SELECT "Correo", "Contrasena", 'admin', "IsDeleted" FROM "Admins";

                INSERT INTO "Cuentas" ("Correo", "Contrasena", "Rol", "IsDeleted")
                SELECT "Correo", "Contrasena", 'profesor', "IsDeleted" FROM "Profesores";

                INSERT INTO "Cuentas" ("Correo", "Contrasena", "Rol", "IsDeleted")
                SELECT "Correo", "Contrasena", 'alumno', "IsDeleted" FROM "Estudiantes";

                UPDATE "Admins" a
                SET "CuentaId" = c."Id"
                FROM "Cuentas" c
                WHERE c."Correo" = a."Correo" AND c."Rol" = 'admin';

                UPDATE "Profesores" p
                SET "CuentaId" = c."Id"
                FROM "Cuentas" c
                WHERE c."Correo" = p."Correo" AND c."Rol" = 'profesor';

                UPDATE "Estudiantes" e
                SET "CuentaId" = c."Id"
                FROM "Cuentas" c
                WHERE c."Correo" = e."Correo" AND c."Rol" = 'alumno';
                """);

            migrationBuilder.AlterColumn<int>(
                name: "CuentaId",
                table: "Profesores",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CuentaId",
                table: "Estudiantes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CuentaId",
                table: "Admins",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Profesores_Correo",
                table: "Profesores");

            migrationBuilder.DropIndex(
                name: "IX_Estudiantes_Correo",
                table: "Estudiantes");

            migrationBuilder.DropIndex(
                name: "IX_Admins_Correo",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "Contrasena",
                table: "Profesores");

            migrationBuilder.DropColumn(
                name: "Correo",
                table: "Profesores");

            migrationBuilder.DropColumn(
                name: "Contrasena",
                table: "Estudiantes");

            migrationBuilder.DropColumn(
                name: "Correo",
                table: "Estudiantes");

            migrationBuilder.DropColumn(
                name: "Contrasena",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "Correo",
                table: "Admins");

            migrationBuilder.CreateIndex(
                name: "IX_Profesores_CuentaId",
                table: "Profesores",
                column: "CuentaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estudiantes_CuentaId",
                table: "Estudiantes",
                column: "CuentaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Admins_CuentaId",
                table: "Admins",
                column: "CuentaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_Correo",
                table: "Cuentas",
                column: "Correo",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.AddForeignKey(
                name: "FK_Admins_Cuentas_CuentaId",
                table: "Admins",
                column: "CuentaId",
                principalTable: "Cuentas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Estudiantes_Cuentas_CuentaId",
                table: "Estudiantes",
                column: "CuentaId",
                principalTable: "Cuentas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Profesores_Cuentas_CuentaId",
                table: "Profesores",
                column: "CuentaId",
                principalTable: "Cuentas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Re-add credential columns as nullable first so we can backfill them.
            migrationBuilder.AddColumn<string>(
                name: "Contrasena",
                table: "Profesores",
                type: "text",
                nullable: true,
                defaultValue: null);

            migrationBuilder.AddColumn<string>(
                name: "Correo",
                table: "Profesores",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                defaultValue: null);

            migrationBuilder.AddColumn<string>(
                name: "Contrasena",
                table: "Estudiantes",
                type: "text",
                nullable: true,
                defaultValue: null);

            migrationBuilder.AddColumn<string>(
                name: "Correo",
                table: "Estudiantes",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                defaultValue: null);

            migrationBuilder.AddColumn<string>(
                name: "Contrasena",
                table: "Admins",
                type: "text",
                nullable: true,
                defaultValue: null);

            migrationBuilder.AddColumn<string>(
                name: "Correo",
                table: "Admins",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                defaultValue: null);

            // 2. Backfill credentials from Cuentas before we drop the FK and the table.
            migrationBuilder.Sql(
                """
                UPDATE "Admins" a
                SET "Correo" = c."Correo", "Contrasena" = c."Contrasena"
                FROM "Cuentas" c
                WHERE c."Id" = a."CuentaId";

                UPDATE "Profesores" p
                SET "Correo" = c."Correo", "Contrasena" = c."Contrasena"
                FROM "Cuentas" c
                WHERE c."Id" = p."CuentaId";

                UPDATE "Estudiantes" e
                SET "Correo" = c."Correo", "Contrasena" = c."Contrasena"
                FROM "Cuentas" c
                WHERE c."Id" = e."CuentaId";
                """);

            // 3. Now drop FKs, indexes, the Cuentas table and the CuentaId columns.
            migrationBuilder.DropForeignKey(
                name: "FK_Admins_Cuentas_CuentaId",
                table: "Admins");

            migrationBuilder.DropForeignKey(
                name: "FK_Estudiantes_Cuentas_CuentaId",
                table: "Estudiantes");

            migrationBuilder.DropForeignKey(
                name: "FK_Profesores_Cuentas_CuentaId",
                table: "Profesores");

            migrationBuilder.DropIndex(
                name: "IX_Profesores_CuentaId",
                table: "Profesores");

            migrationBuilder.DropIndex(
                name: "IX_Estudiantes_CuentaId",
                table: "Estudiantes");

            migrationBuilder.DropIndex(
                name: "IX_Admins_CuentaId",
                table: "Admins");

            migrationBuilder.DropTable(
                name: "Cuentas");

            migrationBuilder.DropColumn(
                name: "CuentaId",
                table: "Profesores");

            migrationBuilder.DropColumn(
                name: "CuentaId",
                table: "Estudiantes");

            migrationBuilder.DropColumn(
                name: "CuentaId",
                table: "Admins");

            // 4. Make the restored columns NOT NULL (they are filled for every row).
            migrationBuilder.AlterColumn<string>(
                name: "Contrasena",
                table: "Profesores",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Correo",
                table: "Profesores",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Contrasena",
                table: "Estudiantes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Correo",
                table: "Estudiantes",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Contrasena",
                table: "Admins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Correo",
                table: "Admins",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldNullable: true);

            // 5. Restore unique indexes on the individual tables.
            migrationBuilder.CreateIndex(
                name: "IX_Profesores_Correo",
                table: "Profesores",
                column: "Correo",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Estudiantes_Correo",
                table: "Estudiantes",
                column: "Correo",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Admins_Correo",
                table: "Admins",
                column: "Correo",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }
    }
}
