using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Teste.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpresaIdToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Projects",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Projects");
        }
    }
}
