using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Back.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHorarioAsignatura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HorariosAsignaturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AsignaturaId = table.Column<int>(type: "integer", nullable: false),
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Aula = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosAsignaturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorariosAsignaturas_Asignaturas_AsignaturaId",
                        column: x => x.AsignaturaId,
                        principalTable: "Asignaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HorariosAsignaturas_AsignaturaId_DiaSemana_HoraInicio",
                table: "HorariosAsignaturas",
                columns: new[] { "AsignaturaId", "DiaSemana", "HoraInicio" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HorariosAsignaturas");
        }
    }
}
