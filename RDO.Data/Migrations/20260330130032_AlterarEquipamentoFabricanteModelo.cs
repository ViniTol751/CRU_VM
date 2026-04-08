using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDO.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlterarEquipamentoFabricanteModelo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Descricao",
                table: "EquipamentosCadastrados",
                newName: "Modelo");

            migrationBuilder.RenameColumn(
                name: "Categoria",
                table: "EquipamentosCadastrados",
                newName: "Fabricante");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Modelo",
                table: "EquipamentosCadastrados",
                newName: "Descricao");

            migrationBuilder.RenameColumn(
                name: "Fabricante",
                table: "EquipamentosCadastrados",
                newName: "Categoria");
        }
    }
}
