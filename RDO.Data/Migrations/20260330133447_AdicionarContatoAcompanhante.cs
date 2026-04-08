using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDO.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarContatoAcompanhante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Contato",
                table: "Acompanhantes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Contato",
                table: "Acompanhantes");
        }
    }
}
