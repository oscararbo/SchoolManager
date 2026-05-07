using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddColegioBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorPrimario",
                table: "Colegios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaviconUrl",
                table: "Colegios",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MensajeLogin",
                table: "Colegios",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorPrimario",
                table: "Colegios");

            migrationBuilder.DropColumn(
                name: "FaviconUrl",
                table: "Colegios");

            migrationBuilder.DropColumn(
                name: "MensajeLogin",
                table: "Colegios");
        }
    }
}
