using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfilExtendido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Apellidos",
                table: "Profesores",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DNI",
                table: "Profesores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Especialidad",
                table: "Profesores",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Profesores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Apellidos",
                table: "Estudiantes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DNI",
                table: "Estudiantes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaNacimiento",
                table: "Estudiantes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Estudiantes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Apellidos",
                table: "Profesores");

            migrationBuilder.DropColumn(
                name: "DNI",
                table: "Profesores");

            migrationBuilder.DropColumn(
                name: "Especialidad",
                table: "Profesores");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Profesores");

            migrationBuilder.DropColumn(
                name: "Apellidos",
                table: "Estudiantes");

            migrationBuilder.DropColumn(
                name: "DNI",
                table: "Estudiantes");

            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "Estudiantes");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Estudiantes");
        }
    }
}
