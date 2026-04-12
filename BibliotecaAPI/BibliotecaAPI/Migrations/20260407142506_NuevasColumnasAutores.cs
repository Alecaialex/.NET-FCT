using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaAPI.Migrations
{
    /// <inheritdoc />
    public partial class NuevasColumnasAutores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Autores");

            migrationBuilder.AddColumn<string>(
                name: "Apellidos",
                table: "Autores",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Identificacion",
                table: "Autores",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nombres",
                table: "Autores",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Apellidos",
                table: "Autores");

            migrationBuilder.DropColumn(
                name: "Identificacion",
                table: "Autores");

            migrationBuilder.DropColumn(
                name: "Nombres",
                table: "Autores");

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Autores",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
