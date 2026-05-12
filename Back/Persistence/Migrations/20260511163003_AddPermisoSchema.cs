using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Back.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermisoSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PermisosSistema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Clave = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermisosSistema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolesSistemaPermisos",
                columns: table => new
                {
                    RolSistemaId = table.Column<int>(type: "integer", nullable: false),
                    PermisoSistemaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolesSistemaPermisos", x => new { x.RolSistemaId, x.PermisoSistemaId });
                    table.ForeignKey(
                        name: "FK_RolesSistemaPermisos_PermisosSistema_PermisoSistemaId",
                        column: x => x.PermisoSistemaId,
                        principalTable: "PermisosSistema",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolesSistemaPermisos_RolesSistema_RolSistemaId",
                        column: x => x.RolSistemaId,
                        principalTable: "RolesSistema",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "PermisosSistema",
                columns: new[] { "Id", "Clave", "Descripcion" },
                values: new object[,]
                {
                    { 1, "colegios.manage", "Gestion de colegios" },
                    { 2, "usuarios.admin.manage", "Gestion de administradores" },
                    { 3, "profesores.manage", "Gestion de profesores" },
                    { 4, "estudiantes.manage", "Gestion de estudiantes" },
                    { 5, "asignaturas.manage", "Gestion de asignaturas" },
                    { 6, "notas.manage", "Gestion de notas" },
                    { 7, "tareas.manage", "Gestion de tareas" },
                    { 8, "panel.alumno.read", "Lectura del panel de alumno" }
                });

            migrationBuilder.InsertData(
                table: "RolesSistemaPermisos",
                columns: new[] { "PermisoSistemaId", "RolSistemaId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 },
                    { 3, 1 },
                    { 4, 1 },
                    { 5, 1 },
                    { 6, 1 },
                    { 7, 1 },
                    { 8, 1 },
                    { 3, 2 },
                    { 4, 2 },
                    { 5, 2 },
                    { 6, 2 },
                    { 7, 2 },
                    { 6, 3 },
                    { 7, 3 },
                    { 8, 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PermisosSistema_Clave",
                table: "PermisosSistema",
                column: "Clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolesSistemaPermisos_PermisoSistemaId",
                table: "RolesSistemaPermisos",
                column: "PermisoSistemaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolesSistemaPermisos");

            migrationBuilder.DropTable(
                name: "PermisosSistema");
        }
    }
}
