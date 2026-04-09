using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteIsDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Admins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Asignaturas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Cursos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EstudianteAsignaturas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Estudiantes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Notas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProfesorAsignaturaCursos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Profesores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RefreshTokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Tareas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.DropIndex(
                name: "IX_Admins_Correo",
                table: "Admins");

            migrationBuilder.DropIndex(
                name: "IX_Asignaturas_CursoId_Nombre",
                table: "Asignaturas");

            migrationBuilder.DropIndex(
                name: "IX_Estudiantes_Correo",
                table: "Estudiantes");

            migrationBuilder.DropIndex(
                name: "IX_Notas_EstudianteId_TareaId",
                table: "Notas");

            migrationBuilder.DropIndex(
                name: "IX_Profesores_Correo",
                table: "Profesores");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens");

            migrationBuilder.CreateIndex(
                name: "IX_Admins_Correo",
                table: "Admins",
                column: "Correo",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaturas_CursoId_Nombre",
                table: "Asignaturas",
                columns: new[] { "CursoId", "Nombre" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Estudiantes_Correo",
                table: "Estudiantes",
                column: "Correo",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Notas_EstudianteId_TareaId",
                table: "Notas",
                columns: new[] { "EstudianteId", "TareaId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Profesores_Correo",
                table: "Profesores",
                column: "Correo",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Admins_Correo",
                table: "Admins");

            migrationBuilder.DropIndex(
                name: "IX_Asignaturas_CursoId_Nombre",
                table: "Asignaturas");

            migrationBuilder.DropIndex(
                name: "IX_Estudiantes_Correo",
                table: "Estudiantes");

            migrationBuilder.DropIndex(
                name: "IX_Notas_EstudianteId_TareaId",
                table: "Notas");

            migrationBuilder.DropIndex(
                name: "IX_Profesores_Correo",
                table: "Profesores");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Asignaturas");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EstudianteAsignaturas");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Estudiantes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Notas");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProfesorAsignaturaCursos");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Profesores");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Tareas");

            migrationBuilder.CreateIndex(
                name: "IX_Admins_Correo",
                table: "Admins",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asignaturas_CursoId_Nombre",
                table: "Asignaturas",
                columns: new[] { "CursoId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estudiantes_Correo",
                table: "Estudiantes",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notas_EstudianteId_TareaId",
                table: "Notas",
                columns: new[] { "EstudianteId", "TareaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profesores_Correo",
                table: "Profesores",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);
        }
    }
}
